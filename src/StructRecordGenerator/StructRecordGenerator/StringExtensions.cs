using Microsoft.CodeAnalysis;

namespace StructRecordGenerators
{
    public static class StringExtensions
    {
        public static bool IsNotNullOrEmpty(this string s) => !string.IsNullOrEmpty(s);

        public static string ReplaceTypeNameAndKeywordInTemplate(this string template, INamedTypeSymbol typeSymbol)
        {
            string typeKeyword = typeSymbol.IsValueType ? "struct" : (typeSymbol.IsRecord() ? "record" : "class");
            string typeName = typeSymbol.ToMinimallyQualifiedFormat();
            return template
                .Replace("$$TYPE_DECLARATION_KEYWORD$$", typeKeyword)
                .Replace("$$TYPE_NAME$$", typeName);
        }
    }
}