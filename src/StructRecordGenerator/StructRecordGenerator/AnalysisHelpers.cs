using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StructRecordGenerators
{
    public static class AnalysisHelpers
    {
        public static bool IsGetSetAutoProperty(this BasePropertyDeclarationSyntax property)
            => property.HasGetterAndSetter() && property.IsAutoProperty();

        public static bool IsEqualityContract(this IPropertySymbol property)
        {
            if (property.Name == "EqualityContract" && property.DeclaringSyntaxReferences.Length == 0)
            {
                return true;
            }
            
            return false;
        }

        public static bool IsAutoProperty(this IPropertySymbol property)
        {
            var syntax = property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (syntax is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                return propertyDeclarationSyntax.IsAutoProperty() || propertyDeclarationSyntax.IsGetSetAutoProperty();
            }

            if (syntax is ParameterSyntax)
            {
                // This is a parameter-like property declaration for records
                // They're not auto properties.
                return false;
            }

            // We don't know what it is!
            return false;
        }
        
        public static bool IsAutoProperty(this BasePropertyDeclarationSyntax syntax)
        {
            bool isAutoProperty = true;
            if (syntax.AccessorList != null)
            {
                foreach (var accessor in syntax.AccessorList.Accessors)
                {
                    if (accessor.Body != null || accessor.ExpressionBody != null)
                    {
                        isAutoProperty = false;
                    }
                }
            }
            else
            {
                isAutoProperty = false;
            }

            return isAutoProperty;
        }

        public static bool HasGetterAndSetter(this BasePropertyDeclarationSyntax property)
            => property.AccessorList?.Accessors.Count == 2;
    }
}
