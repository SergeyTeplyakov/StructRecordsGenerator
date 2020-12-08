using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#nullable enable

namespace StructRecordGenerator
{
    [Generator]
    public class StructEqualityGenerator : ISourceGenerator
    {
        public const string StructAlreadyImplementsEqualityMemberId = "SEG002";

        private static readonly DiagnosticDescriptor StructAlreadyImplementsEqualityMemberlDiagnostic = new DiagnosticDescriptor(
            StructAlreadyImplementsEqualityMemberId,
            "A struct already implements equality member",
            "A struct '{0}' already implements equality member '{1}'",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Info, // I don't think this is super critical, so lets keep it as Info
            isEnabledByDefault: true);

        private const string attributeText = @"
using System;
namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class StructEqualityAttribute : Attribute
    {
    }
}
";

        private const string _classTemplate = @"
using System;
partial struct $$STRUCT_NAME$$ : IEquatable<$$STRUCT_NAME$$>
{
    $$STRUCT_MEMBERS$$
}
";
        private const string _objectEqualsTemplate = @"
/// <inheritdoc/>
public override bool Equals(object obj)
{
    if (obj is $$STRUCT_NAME$$ other)
    {
        return Equals(other);
    }

    return false;
}";

        private const string _equatableEqualsTemplate = @"
/// <inheritdoc/>
public bool Equals($$STRUCT_NAME$$ other)
{
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
/// The equality operator <code>==</code> returns <code>true</code> if its operands are equal, <code>false</code> otherwise. 
/// </summary>
public static bool operator ==($$STRUCT_NAME$$ left, $$STRUCT_NAME$$ right) => $$OPERATOR==IMPL$$;";

        private const string _operatorNotEqualsTemplate = @"
/// <summary>
/// The inequality operator <code>!=</code> returns <code>true</code> if its operands are not equal, <code>false</code> otherwise. 
/// </summary>
public static bool operator !=($$STRUCT_NAME$$ left, $$STRUCT_NAME$$ right) 
    => !(left == right);
";

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("StructEqualityAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            if (context.Compilation is not CSharpCompilation csharpCompilation || csharpCompilation.SyntaxTrees.FirstOrDefault()?.Options is not CSharpParseOptions options)
            {
                return;
            }

            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("StructGenerators.StructEqualityAttribute")!;

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<(StructDeclarationSyntax syntax, INamedTypeSymbol symbol)> annotatedStructs = new ();
            foreach (var structDeclaration in receiver.CandidateStructs)
            {
                SemanticModel model = compilation.GetSemanticModel(structDeclaration.SyntaxTree);
                var structSymbol = model.GetDeclaredSymbol(structDeclaration);
                if (structSymbol is INamedTypeSymbol ts && ts.HasAttribute(attributeSymbol))
                {
                    annotatedStructs.Add((syntax: structDeclaration, symbol: ts));
                }
            }

            foreach(var (syntax, symbol) in annotatedStructs)
            {
                // Need a full name because in one project there could be more than one struct with the same name.
                string structName = symbol.ToDisplayString(FullyQualifiedFormat);

                if (!generateDiagnosticsIfNeeded())
                {
                    var source = GenerateEquality(symbol);
                    context.AddSource($"{structName}_equality.cs", source);
                }

                bool generateDiagnosticsIfNeeded()
                {
                    // Warn if the struct is not partial.
                    if (StructGeneratorAnalyzer.TryCreateStructIsNotPartialDiagnostic(syntax, symbol, context.CancellationToken, out var diagnostic))
                    {
                        context.ReportDiagnostic(diagnostic);
                        return true;
                    }

                    // Warn if the struct already implements any equality members
                    IMethodSymbol[] equalityMembers = GetEqualityMembers(symbol);
                    foreach (var member in equalityMembers)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(StructAlreadyImplementsEqualityMemberlDiagnostic, location: member.DeclaringSyntaxReferences.First().GetSyntax().GetLocation(), structName, member.Name));
                    }

                    return false;
                }
            }
        }

        public static SymbolDisplayFormat FullyQualifiedFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private static IMethodSymbol[] GetEqualityMembers(INamedTypeSymbol structSymbol)
            => structSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => IsEqualityMember(m)).ToArray();

        private static bool IsEqualityMember(IMethodSymbol methodSymbol)
        {
            return IsObjectEqualsOverride(methodSymbol) ||
                IsObjectGetHashCodeOverride(methodSymbol) ||
                IsIEqualityEquals(methodSymbol) ||
                IsEqualityOperator(methodSymbol) ||
                IsInequalityOperator(methodSymbol);
        }

        private static bool IsObjectEqualsOverride(IMethodSymbol methodSymbol) => methodSymbol.IsOverride && methodSymbol.Name == nameof(Equals);

        private static bool IsObjectGetHashCodeOverride(IMethodSymbol methodSymbol) => methodSymbol.IsOverride && methodSymbol.Name == nameof(GetHashCode);

        private static bool IsIEqualityEquals(IMethodSymbol methodSymbol) => (methodSymbol.IsInterfaceImplementation() && methodSymbol.Name == nameof(Equals));

        private static bool IsEqualityOperator(IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Equality";

        private static bool IsInequalityOperator(IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Inequality";

        private string GenerateEquality(INamedTypeSymbol annotatedStruct)
        {
            var fields = annotatedStruct.GetMembers().Where(m => !m.IsStatic && m.Kind == SymbolKind.Field && !m.IsImplicitlyDeclared).ToList();
            var properties = annotatedStruct
                .GetMembers()
                .Where(m => !m.IsStatic && m.Kind == SymbolKind.Property)
                .Select(s => new { Symbol = s, Syntax = s.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax })
                .Where(s => s.Syntax != null)
                .Where(p => p.Syntax.IsAutoProperty() || p.Syntax.IsGetSetAutoProperty())
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
            var existingEqualityMembers = GetEqualityMembers(annotatedStruct);
            
            string structName = annotatedStruct.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            var equalityMembersMap = new Dictionary<string, (bool exists, Func<string, string> replacer)>
            {
                { _objectEqualsTemplate, (existingEqualityMembers.Any(IsObjectEqualsOverride), replaceTemplate)},
                { _equatableEqualsTemplate, (existingEqualityMembers.Any(IsIEqualityEquals), replaceTemplate) },
                { _getHashCodeTemplate, (existingEqualityMembers.Any(IsObjectGetHashCodeOverride), replaceTemplate) },
                { _operatorEqualsTemplate, (existingEqualityMembers.Any(IsEqualityOperator), replaceOperatorEquals) },
                { _operatorNotEqualsTemplate, (existingEqualityMembers.Any(IsInequalityOperator), replaceTemplate) }
            };

            var equalityMembers = equalityMembersMap.Where(kvp => !kvp.Value.exists).Select(kvp => (code: kvp.Key, kvp.Value.exists, kvp.Value.replacer)).ToArray();

            var classBody = string.Join(Environment.NewLine, equalityMembers.Select(tpl => tpl.replacer(tpl.code)));
            var classDeclaration = replaceTemplate(_classTemplate).Replace("$$STRUCT_MEMBERS$$", classBody);

            // The struct can be in a top-level (i.e. global) namespace.
            // Adding namespace only when a struct is declared in one.
            if (!annotatedStruct.ContainingNamespace.IsGlobalNamespace)
            {
                classDeclaration = $"namespace {annotatedStruct.ContainingNamespace.ToDisplayString()} {{ {classDeclaration} }}";
            }

            // Parsing the output and normalizing the whitespaces.
            // This gives us another level of protection against bugs, because the parsing will fail if the string is malformed.
            var parsedOutput = ParseCompilationUnit(classDeclaration);

            return parsedOutput.NormalizeWhitespace().ToFullString();

            string replaceTemplate(string template) =>
                template
                    .Replace("$$STRUCT_NAME$$", structName)
                    .Replace($"$$FIELDS$$", thisMembers)
                    .Replace($"$$LEFT_FIELDS$$", leftMembers)
                    .Replace($"$$RIGHT_FIELDS$$", rightMembers)
                    .Replace($"$$OTHER_FIELDS$$", otherMembers);

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
                    var type = p is IFieldSymbol fs ? fs.Type : (p is IPropertySymbol ps ? ps.Type : null);
                    var supportOperatorEquals = type?.IsOperatorEqualsSupported() ?? false;
                    return (symbol: p, type, name: p.Name, supportOperatorEquals);
                }).ToList();


                var withEquality = membersWithMeta.Where(m => m.supportOperatorEquals).ToList();
                var withoutEquality = membersWithMeta.Where(m => !m.supportOperatorEquals).ToList();

                string first = $"({string.Join(", ", withEquality.Select(m => $"left.{m.name}"))}) == ({string.Join(", ", withEquality.Select(m => $"right.{m.name}"))})";
                string second = $"({string.Join(", ", withoutEquality.Select(m => $"left.{m.name}"))}).Equals(({string.Join(", ", withoutEquality.Select(m => $"right.{m.name}"))}))";

                string body = (withEquality.Count, withoutEquality.Count) switch
                {
                    ( > 0, > 0) => $"{first} && {second}",
                    ( > 0, 0) => first,
                    (0, > 0) => second,
                    (_, _) => "left.Equals(right)",
                };

                return template.Replace("$$OPERATOR==IMPL$$", body);
            }
        }

        /// <summary>
        /// Created on demand before each generation pass.
        /// </summary>
        /// <remarks>
        /// This class should be very efficient.
        /// </remarks>
        internal class SyntaxReceiver : ISyntaxReceiver
        {
            public List<StructDeclarationSyntax> CandidateStructs { get; } = new List<StructDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is StructDeclarationSyntax structDeclarationSyntax && structDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateStructs.Add(structDeclarationSyntax);
                }
            }
        }
    }
}
