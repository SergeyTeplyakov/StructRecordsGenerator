using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StructRecordGenerators.Generators
{
    [Generator]
    public class StructRecordGenerator : TypeMembersGenerator
    {
        private readonly StructEqualityGenerator _structEqualityGenerator;
        private readonly ToStringGenerator _toStringGenerator;

        private const string AttributeName = "StructRecordAttribute";

        private const string attributeText = @"
using System;
namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class StructRecordAttribute : Attribute
    {
    }
}
";

        private const string _classTemplate = @"
using System;
using System.Text;
partial struct $$STRUCT_NAME$$ : IEquatable<$$STRUCT_NAME$$>
{
    $$RECORD_MEMBERS$$
    $$EQUALITY_MEMBERS$$
    $$TO_STRING_MEMBERS$$
}
";
        private const string _constructorTemplate = @"
/// <summary>
/// Creates an instance of struct $$STRUCT_NAME$$.
/// </summary>
private $$SHORT_STRUCT_NAME$$($$CONSTRUCTOR_ARGUMENTS$$)
{
    $$CONSTRUCTOR_BODY$$
}";

        private const string _withMethodTemplate = @"
/// <summary>
/// Creates a copy of the current instance with a given argument <paramref name=""value"" />.
/// </summary>
public $$STRUCT_NAME$$ With$$MEMBER_NAME$$($$MEMBER_TYPE$$ value)
{
    $$WITH_MEMBER_BODY$$
}";

        private const string _cloneMethodTemplate = @"
/// <summary>
/// Creates a copy of the current instance.
/// </summary>
public $$STRUCT_NAME$$ Clone()
{
    return new $$STRUCT_NAME$$($$CLONE_ARGUMENTS$$);
}";

        /// <nodooc />
        public StructRecordGenerator()
            : base(GeneratedTargetTypeKinds.Struct)
        {
            _structEqualityGenerator = new StructEqualityGenerator();
            _toStringGenerator = new ToStringGenerator();
        }

        /// <inheritdooc />
        protected override (string attributeName, string attributeText) GetAttribute()
        {
            return (AttributeName, attributeText);
        }

        public static bool HasStructRecordAttribute(INamedTypeSymbol symbol, Compilation compilation)
        {
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName($"StructGenerators.{AttributeName}")!;
            return symbol.HasAttribute(attributeSymbol);
        }

        /// <inheritdooc />
        public override IMethodSymbol[] GetExistingMembersToGenerate(INamedTypeSymbol typeSymbol)
        {
            // Not sure how to resolve RS1024 in a different way!
#pragma warning disable RS1024 // Compare symbols correctly
            return _structEqualityGenerator.GetExistingMembersToGenerate(typeSymbol).Union(_toStringGenerator.GetExistingMembersToGenerate(typeSymbol)).ToArray();
#pragma warning restore RS1024 // Compare symbols correctly
        }

        /// <inheritdooc />
        protected override string GenerateClassWithNewMembers(INamedTypeSymbol symbol, Compilation compilation)
        {
            string recordMembers = GenerateRecordMembers(symbol);
            string equalityMembers = GenerateEqualityMembers(symbol);
            string toStringMembers = GenerateToStringMembers(symbol);

            string result = _classTemplate
                .ReplaceTypeNameInTemplate(symbol)
                .Replace("$$RECORD_MEMBERS$$", recordMembers)
                .Replace("$$EQUALITY_MEMBERS$$", equalityMembers)
                .Replace("$$TO_STRING_MEMBERS$$", toStringMembers);

            return result;
        }

        private string GenerateRecordMembers(INamedTypeSymbol symbol)
        {
            var allPrivateFieldsAndProperties = GetNonStaticFieldsAndProperties(symbol).ToList();
            string constructor = GenerateConstructor(symbol, allPrivateFieldsAndProperties);
            string withMembers = GenrateWithMembers(symbol, allPrivateFieldsAndProperties);
            string cloneMethod = GenerateCloneMehtod(symbol, allPrivateFieldsAndProperties);

            return string.Join(Environment.NewLine, constructor, withMembers, cloneMethod);
        }

        private string GenerateConstructor(INamedTypeSymbol symbol, List<ISymbol> fieldsAndProperties)
        {
            if (fieldsAndProperties.Count == 0)
            {
                return string.Empty;
            }

            if (symbol.HasConstructorWith(fieldsAndProperties))
            {
                return string.Empty;
            }

            // This is an ugly version, but it will work:

            string arguments = string.Join(", ", fieldsAndProperties.Select(s => $"{s.ToFullyQualifiedDisplayString()} {s.Name}"));
            StringBuilder body = new StringBuilder();
            foreach (var m in fieldsAndProperties)
            {
                // This will generate the following:
                // private MyStruct(string field, int PropertyName)
                // {
                //     this.field = field;
                //     this.PropertyName = PropertyName;
                // }
                // So we don't convert member names to a canonical argument names.
                body.AppendLine($"this.{m.Name} = {m.Name};");
            }

            return _constructorTemplate.ReplaceTypeNameInTemplate(symbol)
                .Replace("$$SHORT_STRUCT_NAME$$", symbol.Name)
                .Replace("$$CONSTRUCTOR_ARGUMENTS$$", arguments)
                .Replace("$$CONSTRUCTOR_BODY$$", body.ToString());
        }

        private string GenerateCloneMehtod(INamedTypeSymbol symbol, List<ISymbol> allPrivateFieldsAndProperties)
        {
            if (symbol.HasCloneMethod())
            {
                return string.Empty;
            }

            string constructorArgs = string.Join(", ", allPrivateFieldsAndProperties.Select(m => m.Name));
            return _cloneMethodTemplate
                .ReplaceTypeNameInTemplate(symbol)
                .Replace("$$CLONE_ARGUMENTS$$", constructorArgs);
        }

        private string GenrateWithMembers(INamedTypeSymbol symbol, List<ISymbol> allNonStaticFieldsAndProperties)
        {
            // TODO: check that the names are unique?
            // Exclude implicitly implemented members! (should be excluded already btw).
            StringBuilder result = new StringBuilder();

            var template = _withMethodTemplate.ReplaceTypeNameInTemplate(symbol);

            var nonPrivateMembers = allNonStaticFieldsAndProperties.Where(m => m.DeclaredAccessibility != Accessibility.Private).ToList();
            // Generating 'WithXXX' for non-private members only.
            foreach (var member in nonPrivateMembers)
            {
                var withMember = template
                    .Replace("$$MEMBER_TYPE$$", member.ToFullyQualifiedDisplayString())
                    .Replace("$$MEMBER_NAME$$", member.Name)
                    .Replace("$$WITH_MEMBER_BODY$$", getConstructorCall(member));

                result.AppendLine(withMember);
            }

            return result.ToString();

            string getConstructorCall(ISymbol currentFieldOrProperty)
            {
                // Constructor call should use all members.
                var arguments = string.Join(", ", allNonStaticFieldsAndProperties.Select(m => m.Equals(currentFieldOrProperty, SymbolEqualityComparer.Default) ? "value" : $"this.{m.Name}"));
                // Need to use the format with generic type arguments and not just a name.
                return $"return new {symbol.ToMinimallyQualifiedFormat()}({arguments});";
            }
        }

        private string GenerateEqualityMembers(INamedTypeSymbol symbol)
        {
            if (_structEqualityGenerator.CanGenerateBody(symbol, compilation: null))
            {
                return _structEqualityGenerator.GenerateBody(symbol);
            }

            return string.Empty;
        }

        private string GenerateToStringMembers(INamedTypeSymbol symbol)
        {
            if (_toStringGenerator.CanGenerateBody(symbol, compilation: null))
            {
                return _toStringGenerator.GenerateBody(symbol);
            }

            return string.Empty;
        }


        /// <inheritdooc />
        public override bool CanGenerateBody(INamedTypeSymbol typeSymbol, Compilation? compilation)
        {
            // TODO: Use more efficient version!
            var nonStaticFieldsAndProperties = GetNonStaticNonPrivateFieldsAndProperties(typeSymbol);

            // Intentionally passing 'null' as compilation argument, because we want to skip the analysis whether the StructGenerator attribute is defined or not.

            // The generator can do stuff if there are some non-private fields or props, or the other generators can produce something.
            return nonStaticFieldsAndProperties.Any() || _structEqualityGenerator.CanGenerateBody(typeSymbol, compilation: null) || _toStringGenerator.CanGenerateBody(typeSymbol, compilation: null);
        }

        private IEnumerable<ISymbol> GetNonStaticNonPrivateFieldsAndProperties(INamedTypeSymbol typeSymbol)
        {
            return GetNonStaticFieldsAndProperties(typeSymbol).Where(s => s.DeclaredAccessibility != Accessibility.Private);
        }

        private IEnumerable<ISymbol> GetNonStaticFieldsAndProperties(INamedTypeSymbol typeSymbol)
        {
#pragma warning disable RS1024 // Compare symbols correctly
            return typeSymbol.GetNonStaticFields().OfType<ISymbol>()
                    .Union(
                        typeSymbol.GetNonStaticProperties(includeAutoPropertiesOnly: true));
#pragma warning restore RS1024 // Compare symbols correctly

        }
    }
}
