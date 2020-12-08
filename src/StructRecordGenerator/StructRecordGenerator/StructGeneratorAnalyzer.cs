using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Diagnostics.CodeAnalysis;
using System.Threading;

#nullable enable

namespace StructRecordGenerator
{
    /// <summary>
    /// A common analyzer that yields diagnostics if the source code is invalid (like if the struct is not marked 'partial'.
    /// </summary>
    public class StructGeneratorAnalyzer
    {
        public const string StructMustBePartialDiagnosticDiagnosticId = "SEG001";
        private const string Title = "A struct must be partial";
        private const string Message = "A struct '{0}' must be partial";

        private static readonly DiagnosticDescriptor StructMustBePartialDiagnostic = new DiagnosticDescriptor(
            StructMustBePartialDiagnosticDiagnosticId,
            Title,
            Message,
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static bool TryCreateStructIsNotPartialDiagnostic(StructDeclarationSyntax syntax, INamedTypeSymbol symbol, CancellationToken token, [NotNullWhen(true)]out Diagnostic? diagnostic)
        {
            if (!symbol.IsPartial(token))
            {
                string structName = symbol.ToDisplayString();
                diagnostic = Diagnostic.Create(StructMustBePartialDiagnostic, location: syntax.Identifier.GetLocation(), structName);
                return true;
            }

            diagnostic = null;
            return false;
        }
    }
}
