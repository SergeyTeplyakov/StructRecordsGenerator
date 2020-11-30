using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
#nullable enable

namespace StructRecordGenerator
{
    public static class SymbolAnalysisHelpers
    {
        public static bool HasAttribute(this ISymbol? symbol, INamedTypeSymbol attributeSymbol)
        {
            if (symbol == null)
            {
                return false;
            }

            return symbol.GetAttributes().Any(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
        }

        public static bool IsPartial(this ITypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            var syntaxRefs = typeSymbol.DeclaringSyntaxReferences;
            return syntaxRefs.Any(n => ((BaseTypeDeclarationSyntax)n.GetSyntax(cancellationToken)).Modifiers.Any(SyntaxKind.PartialKeyword));
        }

        /// <summary>
        /// Returns true if a given <paramref name="method"/> is an implementation of an interface member.
        /// </summary>
        public static bool IsInterfaceImplementation(this IMethodSymbol method)
            => IsInterfaceImplementation(method, out _);

        /// <summary>
        /// Returns true if a given <paramref name="method"/> is an implementation of an interface member.
        /// </summary>
        public static bool IsInterfaceImplementation(this IMethodSymbol method, [NotNullWhen(true)] out ISymbol? implementedMethod)
        {
            if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                implementedMethod = method;
                return true;
            }

            implementedMethod = null;
            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            var containingType = method.ContainingType;
            var implementedInterfaces = containingType.AllInterfaces;

            foreach (var implementedInterface in implementedInterfaces)
            {
                var implementedInterfaceMembersWithSameName = implementedInterface.GetMembers(method.Name);
                foreach (var implementedInterfaceMember in implementedInterfaceMembersWithSameName)
                {
                    if (method.Equals(containingType.FindImplementationForInterfaceMember(implementedInterfaceMember), SymbolEqualityComparer.Default))
                    {
                        implementedMethod = implementedInterfaceMember;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
