using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StructRecordGenerators.Generators
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateToStringAttribute : Attribute
    {
        /// <summary>
        /// If true, the type name will be printed as part of ToString result.
        /// </summary>
        public bool PrintTypeName { get; set; } = true;
    }

    /// <summary>
    /// Controls the behavior of ToString method for a member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class ToStringImplAttribute : Attribute
    {
        /// <summary>
        /// If true, then the collection is printed by calling ToString on a member instead of printing the content of the collection.
        /// </summary>
        public bool LegacyCollectionsBehavior { get; set; }

        /// <summary>
        /// A number of elements printed for a collection member.
        /// </summary>
        public int MaxElementCount { get; set; } = 100;

        /// <summary>
        /// If true, the member won't be printed as part inside ToString implementation.
        /// </summary>
        public bool Skip { get; set; }
    }

    public record ToStringTypeOptions
    {
        /// <summary>
        /// If true, the type name will be printed as part of ToString result.
        /// </summary>
        public bool PrintTypeName { get; set; } = true;

        /// <summary>
        /// The max length of a string representation.
        /// </summary>
        public int MaxStringLength { get; set; } = 1024;
    }
    
    public record ToStringMemberOptions
    {
        public bool PrintTypeNameForCollections { get; set; } = false;
        public int MaxElementCount{ get; set; } = 100;
        public bool Skip { get; set; } = false;
    }

    public class FooBar
    {
        [ToStringImpl(MaxElementCount = 10_000, LegacyCollectionsBehavior = true)]
        public int X { get; set; }
        
        [ToStringImpl(Skip = true)]
        public int Y { get; set; }
    }

    [Generator]
    public class ToStringGenerator : TypeMembersGenerator
    {
        private const int DefaultMaxElementCount = 100;
        
        private const string attributeText = @"
using System;
namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateToStringAttribute : Attribute
    {
        /// <summary>
        /// If true, the type name will be printed as part of ToString result.
        /// </summary>
        public bool PrintTypeName { get; set; } = true;

        /// <summary>
        /// The max length of a final string representation.
        /// </summary>
        public int MaxStringLength { get; set; } = 1024;
    }

    /// <summary>
    /// Controls the behavior of a generated ToString method for a given member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class ToStringImplAttribute : Attribute
    {
        /// <summary>
        /// If true, then the collection is printed by calling ToString on a member instead of printing the content of the collection.
        /// </summary>
        public bool PrintTypeNameForCollections { get; set; }

        /// <summary>
        /// A number of elements printed for a collection member.
        /// </summary>
        public int MaxElementCount { get; set; } = 100;

        /// <summary>
        /// If true, the member won't be printed as part inside ToString implementation.
        /// </summary>
        public bool Skip { get; set; }
    }
}
";

        private const string _typeTemplate = @"
using System;
using System.Linq;
using System.Text;

partial $$CLASS_OR_STRUCT$$ $$STRUCT_NAME$$
{
    $$TYPE_MEMBERS$$
}
";

        private const string _bodyTemplate = @"
/// <inheritdoc/>
public override string ToString()
{
    var sb = new StringBuilder();
    
    $$PRINT_BODY_TYPE_NAME_PREFIX$$sb.Append(""$$TYPE_NAME$$ "");

    sb.Append(""{ "");

    if (PrintMembers(sb))
    {
        sb.Append("" "");
    }

    sb.Append(""}"");

    $$TO_STRING_RETURN_STATEMENT$$
}

$$MODIFIER$$ bool PrintMembers(StringBuilder sb)
{
    $$PRINT_MEMBERS_BODY$$
}";

        /// <nodoc />
        public ToStringGenerator()
            : base(GeneratedTargetTypeKinds.Class | GeneratedTargetTypeKinds.Struct)
        {

        }

        private ToStringTypeOptions GetToStringTypeOptions(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
        {
            var attribute = typeSymbol.TryGetAttribute(attributeSymbol)!;
            var result = new ToStringTypeOptions();
            
            foreach (var kvp in attribute.NamedArguments)
            {
                if (kvp.Key == nameof(ToStringTypeOptions.PrintTypeName))
                {
                    result.PrintTypeName = kvp.Value.Value is true;
                }
                else if (kvp.Key == nameof(ToStringTypeOptions.MaxStringLength) && kvp.Value.Value is int limit)
                {
                    result.MaxStringLength = limit;
                }
            }

            return result;
        }

        public string GenerateBody(Compilation compilation, INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
        {
            string modifier = typeSymbol.IsValueType || typeSymbol.IsSealed ? "private" : "protected virtual";

            var toStringTypeOptions = GetToStringTypeOptions(typeSymbol, attributeSymbol);
            string typeNamePrintPrefix = toStringTypeOptions.PrintTypeName ? string.Empty : "// ";

            var body = _bodyTemplate
                .Replace("$$TYPE_NAME$$", typeSymbol.Name)
                .Replace("$$MODIFIER$$", modifier)
                .Replace("$$PRINT_BODY_TYPE_NAME_PREFIX$$", typeNamePrintPrefix);

            List<ISymbol> fieldsAndProperties = typeSymbol.GetNonStaticFieldsAndProperties(includeAutoPropertiesOnly: false);
            var membersWithOptions = FilterOutSkippedMembers(compilation, fieldsAndProperties).Where(tpl => tpl.options == null || !tpl.options.Skip).ToList();
            
            var printMembersBody = "return false;";

            if (membersWithOptions.Count != 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < membersWithOptions.Count; i++)
                {
                    ISymbol? m = membersWithOptions[i].symbol;

                    var memberSymbolsType = m.GetSymbolType();
                    var options = membersWithOptions[i].options;
                    if (memberSymbolsType.ImplementsIEnumerableOfT(out var elementType) &&
                        // Excluding string here!
                        memberSymbolsType.SpecialType != SpecialType.System_String &&
                        (options is null || !options.PrintTypeNameForCollections))
                    {
                        var limit = options?.MaxElementCount ?? DefaultMaxElementCount;
                        sb.AppendLine($"sb.Append(\"{m.Name} (limit: {limit}) = \");");
                        // Need to add 'e?.ToString()' for reference types only.
                        string optionalNullabilityMark = elementType.IsReferenceType ? "?" : string.Empty;
                        // Printing the content of the collection (unless configured to use the legacy behavior)
                        sb.AppendLine($"if ({m.Name} is not null)")
                            .AppendLine("{")
                            .AppendLine("sb.Append(\"[\");")
                            .AppendLine($"sb.Append(string.Join(\", \", {m.Name}.Take({limit}).Select(e => e{optionalNullabilityMark}.ToString())));")
                            .AppendLine("sb.Append(\"]\");")
                            .AppendLine("}");
                    }
                    else
                    {
                        sb.AppendLine($"sb.Append(\"{m.Name} = \");");
                        // It is important to generate 'Append((object)memberName)' for reference type fields to avoid failing with NRE if the field is null.
                        // The cast is required to force the call of 'ToString' method on an instance and to avoid overload resolution failures.
                        string castToObject = memberSymbolsType.IsReferenceType ? "(object)" : string.Empty;
                        
                        // Using ?.ToString() for generics to avoid boxing allocation
                        string suffix = memberSymbolsType.TypeKind == TypeKind.TypeParameter ? "?.ToString()" : string.Empty;
                        sb.AppendLine($"sb.Append({castToObject}{m.Name}{suffix});");
                    }

                    if (i != membersWithOptions.Count - 1)
                    {
                        sb.AppendLine("sb.Append(\", \");");
                    }
                }

                sb.AppendLine("return true;");
                printMembersBody = sb.ToString();
            }

            // Limiting the size of the resulting string.
            body = body.Replace("$$TO_STRING_RETURN_STATEMENT$$", $"return sb.ToString(0, Math.Min(sb.Length, /*String rep limit*/{toStringTypeOptions.MaxStringLength}));");

            body = body.Replace("$$PRINT_MEMBERS_BODY$$", printMembersBody);
            return body;
        }

        private List<(ISymbol symbol, ToStringMemberOptions? options)> FilterOutSkippedMembers(Compilation compilation, List<ISymbol> fieldsAndProperties)
        {
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("StructGenerators.ToStringImplAttribute")!;

            return fieldsAndProperties.Select(symbol => (symbol, options: tryGetToStringImplOptions(symbol))).ToList();

            ToStringMemberOptions? tryGetToStringImplOptions(ISymbol symbol)
            {
                var result = new ToStringMemberOptions();
                
                var attribute = symbol.TryGetAttribute(attributeSymbol);
                if (attribute == null)
                {
                    return result;
                }
                
                foreach (var kvp in attribute.NamedArguments)
                {
                    if (kvp.Key == nameof(ToStringMemberOptions.PrintTypeNameForCollections))
                    {
                        result.PrintTypeNameForCollections = kvp.Value.Value is true;
                    }
                    else if (kvp.Key == nameof(ToStringMemberOptions.Skip))
                    {
                        result.Skip = kvp.Value.Value is true;
                    }
                    else if (kvp.Key == nameof(ToStringMemberOptions.MaxElementCount) && kvp.Value.Value is int limit)
                    {
                        result.MaxElementCount = limit;
                    }
                }
                
                return result;
            }
        }

        /// <inheritdoc/>
        public override bool CanGenerateBody(INamedTypeSymbol typeSymbol, Compilation? compilation)
        {
            if (compilation != null && StructRecordGenerator.HasStructRecordAttribute(typeSymbol, compilation))
            {
                // StructRecord attribute is applied, don't need to generate anything.
                return false;
            }

            // If the ToString() is already generated, nothing we can do here.
            if (GetExistingMembersToGenerate(typeSymbol).Length != 0)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override string GenerateClassWithNewMembers(INamedTypeSymbol typeSymbol, Compilation compilation, INamedTypeSymbol attributeSymbol)
        {
            string classOrStruct = typeSymbol.IsValueType ? "struct" : "class";
            var body = GenerateBody(compilation, typeSymbol, attributeSymbol);

            return _typeTemplate
                .ReplaceTypeNameInTemplate(typeSymbol)
                .Replace("$$CLASS_OR_STRUCT$$", classOrStruct)
                .Replace("$$TYPE_MEMBERS$$", body);
        }

        /// <inheritdoc/>
        protected override (string attributeName, string attributeText) GetAttribute()
        {
            return ("GenerateToStringAttribute", attributeText);
        }

        /// <inheritdoc/>
        public override IMethodSymbol[] GetExistingMembersToGenerate(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsObjectToStringOverride()).ToArray();
        }
    }
}
