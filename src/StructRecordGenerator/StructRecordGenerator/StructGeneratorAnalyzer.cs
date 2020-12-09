using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        public const string StructAlreadyImplementsEqualityMemberId = "SEG002";

        public static readonly DiagnosticDescriptor StructAlreadyImplementsEqualityMemberlDiagnostic = new DiagnosticDescriptor(
            StructAlreadyImplementsEqualityMemberId,
            "A struct already implements equality member",
            "A struct '{0}' already implements equality member '{1}'",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Info, // I don't think this is super critical, so lets keep it as Info
            isEnabledByDefault: true);

        public static bool TryCreateStructIsNotPartialDiagnostic(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol, CancellationToken token, [NotNullWhen(true)]out Diagnostic? diagnostic)
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

        public static List<Diagnostic> GetMembersAlreadyExistsDiagnostics(string typeName, IEnumerable<IMethodSymbol> existingMethods)
        {
            return existingMethods.Select(member => 
                    Diagnostic.Create(StructAlreadyImplementsEqualityMemberlDiagnostic,
                                      location: member.DeclaringSyntaxReferences.First().GetSyntax().GetLocation(),
                                      typeName,
                                      member.Name)).ToList();
        }
    }
}
