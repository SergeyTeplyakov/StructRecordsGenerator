using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace StructRecordGenerators
{
    public static class GeneratorsHelpers
    {
        public static string GetGeneratedFileName(this ISymbol typeSymbol, string generatorName)
        {
            // To limit the size of the output name we generate a hash from the full type name and the generator name.
            // And then use just abbreviation from the name of the generator.
            // So t he output file name will look like this:
            // ClassName_TSG_422222.cs
            var fullName = typeSymbol.GetFullName();
            var hash = Hash.GetFNVHashCode(fullName + generatorName);
            return $"{typeSymbol.Name}_{GetAbbreviation(generatorName)}_{hash}.cs";
        }

        public static string GetAbbreviation(string str)
        {
            var result = new List<char>();
            foreach (var c in str)
            {
                if (char.IsUpper(c))
                {
                    result.Add(c);
                }
            }

            return new string(result.ToArray());
        }

        public static string GetFullName(this ISymbol typeName)
        {
            return typeName.ToDisplayString(FullyQualifiedFormat);
        }

        public static SymbolDisplayFormat FullyQualifiedFormat { get; } =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.None,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    }
}