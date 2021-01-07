using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace StructRecordGenerators.Analyzers
{
    /// <summary>
    /// A common analyzer that yields diagnostics if the source code is invalid (like if the struct is not marked 'partial'.
    /// </summary>
    public class StructGeneratorAnalyzer
    {
        public const string StructMustBePartialDiagnosticDiagnosticId = "SRG001";
        private const string Title = "A type must be partial";
        private const string Message = "A type '{0}' must be partial";

        private static readonly DiagnosticDescriptor StructMustBePartialDiagnostic = new DiagnosticDescriptor(
            StructMustBePartialDiagnosticDiagnosticId,
            Title,
            Message,
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public const string StructAlreadyImplementsMemberId = "SRG002";

        public static readonly DiagnosticDescriptor StructAlreadyImplementsEqualityMemberDiagnostic = new DiagnosticDescriptor(
            StructAlreadyImplementsMemberId,
            "A type already implements has a method",
            "A type '{0}' already has method '{1}'",
            category: "Correctness",
            defaultSeverity: DiagnosticSeverity.Info, // I don't think this is super critical, so lets keep it as Info
            isEnabledByDefault: true);

        public static bool TryCreateTypeIsNotPartialDiagnostic(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol, CancellationToken token, [NotNullWhen(true)] out Diagnostic? diagnostic)
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
                    Diagnostic.Create(StructAlreadyImplementsEqualityMemberDiagnostic,
                                      location: member.DeclaringSyntaxReferences.First().GetSyntax().GetLocation(),
                                      typeName,
                                      member.Name)).ToList();
        }
    }
}
