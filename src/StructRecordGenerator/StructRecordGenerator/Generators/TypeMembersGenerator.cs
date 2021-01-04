using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using StructRecordGenerators.Analyzers;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StructRecordGenerators.Generators
{
    public abstract class TypeMembersGenerator : ISourceGenerator
    {
        private readonly GeneratedTargetTypeKinds _targetKinds;

        protected TypeMembersGenerator(GeneratedTargetTypeKinds targetKinds)
        {
            _targetKinds = targetKinds;
        }

        protected abstract (string attributeName, string attributeText) GetAttribute();

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new TypeSyntaxReceiver(_targetKinds));
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            // Retrieve the populated receiver 
            if (context.SyntaxReceiver is not TypeSyntaxReceiver receiver)
            {
                return;
            }

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            if (context.Compilation is not CSharpCompilation csharpCompilation || csharpCompilation.SyntaxTrees.FirstOrDefault()?.Options is not CSharpParseOptions options)
            {
                return;
            }

            var (attributeName, attributeText) = GetAttribute();

            // add the attribute text
            context.AddSource(attributeName, SourceText.From(attributeText, Encoding.UTF8));

            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attribute
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName($"StructGenerators.{attributeName}")!;

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol)> annotatedStructs = new();
            foreach (var structDeclaration in receiver.Candidates)
            {
                SemanticModel model = compilation.GetSemanticModel(structDeclaration.SyntaxTree);
                var structSymbol = model.GetDeclaredSymbol(structDeclaration);
                if (structSymbol is INamedTypeSymbol ts && ts.HasAttribute(attributeSymbol))
                {
                    annotatedStructs.Add((syntax: structDeclaration, symbol: ts));
                }
            }

            foreach (var (syntax, symbol) in annotatedStructs)
            {
                // Need a full name because in one project there could be more than one struct with the same name.
                string typeName = symbol.ToDisplayString(FullyQualifiedFormat);

                // Warn if the struct or class is not partial.
                if (StructGeneratorAnalyzer.TryCreateStructIsNotPartialDiagnostic(syntax, symbol, context.CancellationToken, out var diagnostic))
                {
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                // Warn if the type already implements some members
                var diagnostics = StructGeneratorAnalyzer.GetMembersAlreadyExistsDiagnostics(typeName, GetExistingMembersToGenerate(symbol));
                foreach (var d in diagnostics)
                {
                    context.ReportDiagnostic(d);
                }
                
                if (CanGenerateBody(symbol, compilation))
                {
                    var typeDeclaration = GenerateClassWithNewMembers(symbol, compilation, attributeSymbol);

                    // The struct can be in a top-level (i.e. global) namespace.
                    // Adding namespace only when a struct is declared in one.
                    if (!symbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        typeDeclaration = $"namespace {symbol.ContainingNamespace.ToDisplayString()} {{ {typeDeclaration} }}";
                    }

                    // Parsing the output and normalizing the whitespaces.
                    // This gives us another level of protection against bugs, because the parsing will fail if the string is malformed.
                    var parsedOutput = ParseCompilationUnit(typeDeclaration);

                    string final = parsedOutput.NormalizeWhitespace().ToFullString();
                    context.AddSource($"{typeName}_{GetType().Name}.cs", final);
                }
            }
        }

        protected abstract string GenerateClassWithNewMembers(INamedTypeSymbol symbol, Compilation compilation, INamedTypeSymbol attributeSymbol);

        public abstract IMethodSymbol[] GetExistingMembersToGenerate(INamedTypeSymbol typeSymbol);

        public abstract bool CanGenerateBody(INamedTypeSymbol typeSymbol, Compilation? compilation);

        public static SymbolDisplayFormat FullyQualifiedFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    }
}
