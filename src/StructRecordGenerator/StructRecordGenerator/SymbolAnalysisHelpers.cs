using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

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
        
        public static bool IsIEqualityEquals(this IMethodSymbol methodSymbol) => (methodSymbol.IsInterfaceImplementation() && methodSymbol.Name == nameof(Equals));
        
        public static bool IsEqualityOperator(this IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Equality";
        
        public static bool IsInequalityOperator(this IMethodSymbol methodSymbol) => methodSymbol.MethodKind == MethodKind.UserDefinedOperator && methodSymbol.Name is "op_Inequality";

        public static bool IsEqualityMember(this IMethodSymbol methodSymbol)
        {
            return IsObjectEqualsOverride(methodSymbol) ||
                IsObjectGetHashCodeOverride(methodSymbol) ||
                IsIEqualityEquals(methodSymbol) ||
                IsEqualityOperator(methodSymbol) ||
                IsInequalityOperator(methodSymbol);
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
                .Select(s => new { Symbol = s, Syntax = s.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax })
                .Where(s => s.Syntax != null)
                .Where(p => !includeAutoPropertiesOnly || (p.Syntax.IsAutoProperty() || p.Syntax.IsGetSetAutoProperty()))
                .Select(p => (IPropertySymbol)p.Symbol)
                .ToList();

            return properties;
        }

        public static string ToFullyQualifiedDisplayString(this ISymbol symbol)
        {
            var type = symbol switch
            {
                ITypeSymbol ts => ts,
                IFieldSymbol fs => fs.Type,
                IPropertySymbol ps => ps.Type,
                _ => throw new InvalidOperationException($"Unknown symbol type '{symbol.GetType()}'"),
            };

            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public static string ToMinimallyQualifiedFormat(this INamedTypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }
}
