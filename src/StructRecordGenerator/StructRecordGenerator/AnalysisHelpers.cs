using Microsoft.CodeAnalysis.CSharp.Syntax;
#nullable enable

namespace StructRecordGenerator
{

    public static class AnalysisHelpers
    {
        public static bool IsGetSetAutoProperty(this BasePropertyDeclarationSyntax property)
            => property.HasGetterAndSetter() && property.IsAutoProperty();

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
