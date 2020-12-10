using Microsoft.CodeAnalysis;

namespace StructRecordGenerators
{
    public static class StringExtensions
    {
        public static bool IsNotNullOrEmpty(this string s) => !string.IsNullOrEmpty(s);

        public static string ReplaceTypeNameInTemplate(this string template, INamedTypeSymbol typeSymbol)
        {
            string structName = typeSymbol.ToMinimallyQualifiedFormat();
            return template.Replace("$$STRUCT_NAME$$", structName);
        }
    }
}