using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace StructRecordGenerators
{
    public static class SymbolAnalysisHelpers
    {
        public static bool HasAttribute(this ISymbol? symbol, INamedTypeSymbol attributeSymbol)
        {
            return symbol.TryGetAttribute(attributeSymbol) != null;
        }

        public static bool HasAttributeUnsafe(this ISymbol? symbol, string attributeName)
        {
            if (symbol == null)
            {
                return false;
            }

            return symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Name.Contains(attributeName) == true) != null;
        }

        public static AttributeData? TryGetAttribute(this ISymbol? symbol, INamedTypeSymbol attributeSymbol)
        {
            if (symbol == null)
            {
                return null;
            }

            var result = symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
            return result;
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
            => method.IsInterfaceImplementation(out _);

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

        public static bool IsOperatorEqualsSupported(this ITypeSymbol type)
        {
            if (type is INamedTypeSymbol nts && nts.IsGenericType)
            {
                return false;
            }

            if (type.SpecialType != SpecialType.None)
            {
                // I believe that all special types support operator ==
                return true;
            }

            return type.GetMembers().Any(n => n.Name == "op_Equality");
        }

        public static bool IsObjectEqualsOverride(this IMethodSymbol methodSymbol) => methodSymbol.IsOverride && methodSymbol.Name == nameof(Equals);

        public static bool IsObjectToStringOverride(this IMethodSymbol methodSymbol) => methodSymbol.IsOverride && methodSymbol.Name == nameof(ToString);

        public static bool IsObjectGetHashCodeOverride(this IMethodSymbol methodSymbol) => methodSymbol.IsOverride && methodSymbol.Name == nameof(GetHashCode);

        public static bool IsIEqualityEquals(this IMethodSymbol methodSymbol) => methodSymbol.IsInterfaceImplementation() && methodSymbol.Name == nameof(Equals);

        public static bool IsEqualityOperator(this IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Equality";

        public static bool IsInequalityOperator(this IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Inequality";

        public static bool IsClone(this IMethodSymbol methodSymbol) => methodSymbol.Name == "Clone" && methodSymbol.Parameters.IsEmpty;

        public static bool HasCloneMethod(this INamedTypeSymbol type) => type.GetMembers().Any(m => m is IMethodSymbol ms && ms.IsClone());

        public static bool HasConstructorWith(this INamedTypeSymbol type, List<ISymbol> arguments)
        {
            return type.GetMembers().Any(m => m is IMethodSymbol ms && ms.MethodKind == MethodKind.Constructor && ms.IsConstructorWith(arguments));
        }

        public static bool IsRecord(this INamedTypeSymbol type)
        {
            // Not a perfect way, but the only one I've found so far!
            return type.GetMembers().Any(x =>
                x.Kind == SymbolKind.Property && x.Name == "EqualityContract" && x.IsImplicitlyDeclared);
        }

        public static bool ImplementsIEnumerableOfT(this ITypeSymbol type, [NotNullWhen(true)]out ITypeSymbol? elementType)
        {
            var allInterfaces = type.AllInterfaces;

            elementType = null;

            foreach (var i in allInterfaces)
            {
                if (i.MetadataName == "IEnumerable`1" && i.ContainingNamespace.ToString() == "System.Collections.Generic")
                {
                    elementType = i.TypeArguments[0];
                    return true;
                }
            }

            return false;
        }

        public static bool IsConstructorWith(this IMethodSymbol methodSymbol, List<ISymbol> arguments)
        {
#pragma warning disable RS1024 // Compare symbols correctly
            return methodSymbol.Parameters.Select(p => p.Type)
                .SequenceEqual(arguments.Select(a => a.GetSymbolType()));
#pragma warning restore RS1024 // Compare symbols correctly
        }

        public static bool IsEqualityMember(this IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsObjectEqualsOverride() ||
                methodSymbol.IsObjectGetHashCodeOverride() ||
                methodSymbol.IsIEqualityEquals() ||
                methodSymbol.IsEqualityOperator() ||
                methodSymbol.IsInequalityOperator();
        }

        public static List<ISymbol> GetNonStaticFieldsAndProperties(this INamedTypeSymbol type, bool includeAutoPropertiesOnly)
        {
            var fields = type.GetNonStaticFields();
            var properties = type.GetNonStaticProperties(includeAutoPropertiesOnly);

            var fieldsOrProps = fields.OfType<ISymbol>().Concat(properties).ToList();
            return fieldsOrProps;
        }

        public static List<IFieldSymbol> GetNonStaticFields(this INamedTypeSymbol type)
        {
            return type.GetMembers().Where(m => !m.IsStatic && m.Kind == SymbolKind.Field && !m.IsImplicitlyDeclared).Select(m => (IFieldSymbol)m).ToList();
        }

        public static List<IPropertySymbol> GetNonStaticProperties(this INamedTypeSymbol type, bool includeAutoPropertiesOnly)
        {
            var properties = type
                .GetMembers()
                .Where(m => !m.IsStatic && m.Kind == SymbolKind.Property)
                .Select(p => (IPropertySymbol)p)
                .Where(p => !includeAutoPropertiesOnly || p.IsAutoProperty())
                .Where(p => !p.IsEqualityContract())
                .ToList();

            return properties;
        }

        public static string ToFullyQualifiedDisplayString(this ISymbol symbol)
        {
            var type = symbol.GetSymbolType();
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public static ITypeSymbol GetSymbolType(this ISymbol symbol)
        {
            return symbol switch
            {
                ITypeSymbol ts => ts,
                IFieldSymbol fs => fs.Type,
                IPropertySymbol ps => ps.Type,
                _ => throw new InvalidOperationException($"Unknown symbol type '{symbol.GetType()}'"),
            };
        }

        public static string ToMinimallyQualifiedFormat(this INamedTypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }
}
