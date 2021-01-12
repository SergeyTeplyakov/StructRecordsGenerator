using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Linq;
using StructGenerators;
using StructRecordGenerators.Properties;

namespace StructRecordGenerators.Generators
{
    [Generator]
    public class EqualityGenerator : TypeMembersGenerator
    {
        private const string _typeTemplate = @"
using System;
#nullable disable // Have to disable it because Equals(object other) is different whether nullability is on or off.
partial $$TYPE_DECLARATION_KEYWORD$$ $$TYPE_NAME$$ : IEquatable<$$TYPE_NAME$$>
{
    $$TYPE_MEMBERS$$
}
";
        private const string _objectEqualsTemplate = @"
/// <inheritdoc/>
public override bool Equals(object obj)
{
    if (obj is $$TYPE_NAME$$ other)
    {
        return Equals(other);
    }

    return false;
}";

        private const string _equalityContract = @"
/// <summary>
/// An equality contract used for determining that two instances are of the same type.
/// </summary>
$$EQUALITY_CONTRACT_MODIFIER$$ Type EqualityContract
{
    get
    {
        return typeof($$TYPE_NAME$$);
    }
}
";

        private const string _equatableEqualsTemplate = @"
/// <inheritdoc/>
public bool Equals($$TYPE_NAME$$ other)
{$$OPTIONAL_CLASS_EQUALITY_CHECK$$
    return ($$FIELDS$$).Equals(($$OTHER_FIELDS$$));
}";

        private const string _getHashCodeTemplate = @"
/// <inheritdoc/>
public override int GetHashCode()
{
    return ($$FIELDS$$).GetHashCode();
}";

        // Technically, this implementation is not 100% correct, because .Equals behavior may differ from == behavior (for instance, for Double).
        private const string _operatorEqualsTemplate = @"
/// <summary>
/// The equality operator <c>==</c> returns <c>true</c> if its operands are equal, <c>false</c> otherwise. 
/// </summary>
public static bool operator ==($$TYPE_NAME$$ left, $$TYPE_NAME$$ right) => $$OPERATOR==IMPL$$;";

        private const string _operatorNotEqualsTemplate = @"
/// <summary>
/// The inequality operator <c>!=</c> returns <c>true</c> if its operands are not equal, <c>false</c> otherwise. 
/// </summary>
public static bool operator !=($$TYPE_NAME$$ left, $$TYPE_NAME$$ right) 
    => !(left == right);
";

        public EqualityGenerator()
            : base(GeneratedTargetTypeKinds.Struct | GeneratedTargetTypeKinds.Class)
        { }

        /// <inheritdoc />
        protected override (string attributeName, string attributeText) GetAttribute()
        {
            return (nameof(GenerateEqualityAttribute), Resources.GenerateEqualityAttribute);
        }

        /// <inheritdoc />
        public override IMethodSymbol[] GetExistingMembersToGenerate(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsEqualityMember()).ToArray();
        }

        /// <inheritdoc />
        public override bool CanGenerateBody(INamedTypeSymbol typeSymbol, Compilation? compilation)
        {
            if (compilation != null && StructRecordGenerator.HasStructRecordAttribute(typeSymbol, compilation))
            {
                // StructRecord attribute is applied, don't need to generate anything.
                return false;
            }

            // 5 is the number of equality members.
            return GetExistingMembersToGenerate(typeSymbol).Length < 5;
        }

        /// <inheritdoc />
        protected override string GenerateClassWithNewMembers(INamedTypeSymbol symbol, Compilation compilation, INamedTypeSymbol attributeSymbol)
        {
            return GenerateEquality(symbol);
        }

        public static bool HasGenerateStructEqualityAttribute(INamedTypeSymbol symbol, Compilation compilation)
        {
            // I can't see a very simple way how to change the functionality when another generator will be used.
            // For instance, the type marked with [StructRecord] and [GenerateToString]
            // when StructRecordGenerator generator runs the attribute 'GenerateToStringAttribute'
            // is not defined as part of the compilation, because the generators are isolated from each other.
            // So there is no simple way to check inside generator 1 that generator 2 will be running as well.
            // At least its not clear how to do in a fully correct symbol-based.
            var attributeName = nameof(GenerateEqualityAttribute).Replace("Attribute", string.Empty);
            return symbol.HasAttributeUnsafe(attributeName);
        }

        public string GenerateBody(INamedTypeSymbol typeSymbol)
        {
            var fields = typeSymbol.GetMembers().Where(m => !m.IsStatic && m.Kind == SymbolKind.Field && !m.IsImplicitlyDeclared).ToList();
            var properties = typeSymbol
                .GetMembers()
                .Where(m => !m.IsStatic && m.Kind == SymbolKind.Property)
                .Select(s => new { Symbol = s, Syntax = s.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax })
                .Where(s => s.Syntax != null)
                .Where(p => p.Syntax!.IsAutoProperty() || p.Syntax.IsGetSetAutoProperty())
                .Select(p => p.Symbol)
                .ToList();

            var fieldsOrProps = fields.Concat(properties).ToList();

            var thisMembers = string.Join(", ", fieldsOrProps.Select(f => f.Name));
            var otherMembers = string.Join(", ", fieldsOrProps.Select(f => $"other.{f.Name}"));
            var leftMembers = string.Join(", ", fieldsOrProps.Select(f => $"left.{f.Name}"));
            var rightMembers = string.Join(", ", fieldsOrProps.Select(f => $"right.{f.Name}"));

            // Need to handle differently the case when the struct has no fields.
            // In this case, just use '42' as a placeholder for the state.
            if (fieldsOrProps.Count == 0)
            {
                thisMembers = otherMembers = leftMembers = rightMembers = "42";
            }

            // Need to check which equality members to generate based on existing members.
            var existingEqualityMembers = GetExistingMembersToGenerate(typeSymbol);

            var equalityMembersMap = new Dictionary<string, (bool exists, Func<string, string> replacer)>
            {
                { _objectEqualsTemplate, (existingEqualityMembers.Any(SymbolAnalysisHelpers.IsObjectEqualsOverride), replaceTemplate)},
                { _equatableEqualsTemplate, (existingEqualityMembers.Any(SymbolAnalysisHelpers.IsIEqualityEquals), replaceEqualsMethod) },
                { _getHashCodeTemplate, (existingEqualityMembers.Any(SymbolAnalysisHelpers.IsObjectGetHashCodeOverride), replaceTemplate) },
                { _operatorEqualsTemplate, (existingEqualityMembers.Any(SymbolAnalysisHelpers.IsEqualityOperator), replaceOperatorEquals) },
                { _operatorNotEqualsTemplate, (existingEqualityMembers.Any(SymbolAnalysisHelpers.IsInequalityOperator), replaceTemplate) }
            };

            if (!typeSymbol.IsValueType)
            {
                // inheritance is not supported right now!
                string modifier = typeSymbol.IsSealed ? "private" : "protected virtual";
                var equalityContract = _equalityContract.Replace("$$EQUALITY_CONTRACT_MODIFIER$$", modifier);
                equalityMembersMap.Add(equalityContract, (false, replaceTemplate));
            }
            
            var equalityMembers = equalityMembersMap.Where(kvp => !kvp.Value.exists).Select(kvp => (code: kvp.Key, kvp.Value.exists, kvp.Value.replacer)).ToArray();
            var classBody = string.Join(Environment.NewLine, equalityMembers.Select(tpl => tpl.replacer(tpl.code)));
            return classBody;

            string replaceTemplate(string template) =>
                template.ReplaceTypeNameAndKeywordInTemplate(typeSymbol)
                    .Replace($"$$FIELDS$$", thisMembers)
                    .Replace($"$$LEFT_FIELDS$$", leftMembers)
                    .Replace($"$$RIGHT_FIELDS$$", rightMembers)
                    .Replace($"$$OTHER_FIELDS$$", otherMembers);

            string replaceEqualsMethod(string template)
            {
                template = replaceTemplate(template);

                if (typeSymbol.IsReferenceType)
                {
                    return template.Replace(
                        "$$OPTIONAL_CLASS_EQUALITY_CHECK$$",
                        @"if (other is null || EqualityContract != other.EqualityContract) { return false; }");
                }
                
                return template.Replace("$$OPTIONAL_CLASS_EQUALITY_CHECK$$", string.Empty);
            }
            
            string replaceOperatorEquals(string template)
            {
                template = replaceTemplate(template);

                // Operator== is a bit trickier.
                // Here what we're going to do:
                // we split all the members into two groups to generate the following code:
                // operator==(left, right) => (left.field1, left.field2, left.field3) == (right.field1, right.field2, right.field3) && (left.field4, left.field5).Equals(right.field4, right.field5).
                // The first group is the group of fields that support operator == and the second group that doesn't.
                var membersWithMeta = fieldsOrProps.Select(p =>
                {
                    var type = p is IFieldSymbol fs ? fs.Type : p is IPropertySymbol ps ? ps.Type : null;
                    var supportOperatorEquals = type?.IsOperatorEqualsSupported() ?? false;
                    return (symbol: p, type, name: p.Name, supportOperatorEquals);
                }).ToList();


                var membersWithOperatorSupport = membersWithMeta.Where(m => m.supportOperatorEquals).ToList();
                var membersWithoutOperatorSupport = membersWithMeta.Where(m => !m.supportOperatorEquals).ToList();

                string operatorEquality = $"({string.Join(", ", membersWithOperatorSupport.Select(m => $"left.{m.name}"))}) == ({string.Join(", ", membersWithOperatorSupport.Select(m => $"right.{m.name}"))})";
                string methodEquality = $"({string.Join(", ", membersWithoutOperatorSupport.Select(m => $"left.{m.name}"))}).Equals(({string.Join(", ", membersWithoutOperatorSupport.Select(m => $"right.{m.name}"))}))";

                string body = (membersWithOperatorSupport.Count, membersWithoutOperatorSupport.Count) switch
                {
                    ( > 0, > 0) => $"{operatorEquality} && {methodEquality}",
                    ( > 0, 0) => operatorEquality,
                    (0, > 0) => methodEquality,
                    (_, _) => "left.Equals(right)",
                };

                return template.Replace("$$OPERATOR==IMPL$$", body);
            }
        }

        // TODOC
        public string GenerateEquality(INamedTypeSymbol typeSymbol)
        {
            var classBody = GenerateBody(typeSymbol);
            var classDeclaration = _typeTemplate
                .ReplaceTypeNameAndKeywordInTemplate(typeSymbol)
                .Replace("$$TYPE_MEMBERS$$", classBody);

            return classDeclaration;
        }
    }
}
