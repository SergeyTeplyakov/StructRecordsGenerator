using Microsoft.CodeAnalysis;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;


#nullable enable

namespace StructRecordGenerator
{
    [Generator]
    public class ToStringGenerator : TypeMembersGenerator
    {
        private const string attributeText = @"
using System;
namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateToStringAttribute : Attribute
    {
    }
}
";

        private const string _typeTemplate = @"
using System;
using System.Text;
partial $$CLASS_OR_STRUCT$$ $$TYPE_NAME$$
{
    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(""$$TYPE_NAME$$"");
        sb.Append("" { "");

        if (PrintMembers(sb))
        {
            sb.Append("" "");
        }

        sb.Append(""}"");

        return sb.ToString(); 
    }

    $$MODIFIER$$ bool PrintMembers(StringBuilder sb)
    {
        $$PRINT_MEMBERS_BODY$$
    }
}
";

        /// <nodoc />
        public ToStringGenerator()
            : base(GeneratedTargetTypeKinds.Class | GeneratedTargetTypeKinds.Struct)
        {

        }

        /// <inheritdoc/>
        protected override bool TryGenerateClassWithNewMembers(INamedTypeSymbol symbol, [NotNullWhen(true)] out string? result)
        {
            result = null;
            // If the ToString() is already generated, nothing we can do here.
            if (GetExistingMembersToGenerate(symbol).Length != 0)
            {
                return false;
            }

            string typeName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            string classOrStruct = symbol.IsValueType ? "struct" : "class";
            string modifier = symbol.IsValueType || symbol.IsSealed ? "private" : "protected virtual";
            result = _typeTemplate
                .Replace("$$CLASS_OR_STRUCT$$", classOrStruct)
                .Replace("$$MODIFIER$$", modifier)
                .Replace("$$TYPE_NAME$$", typeName);

            var fieldsAndProperties = symbol.GetNonStaticFieldsAndProperties(includeAutoPropertiesOnly: false);
            
            var printMembersBody = "return false;";

            if (fieldsAndProperties.Count != 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < fieldsAndProperties.Count; i++)
                {
                    ISymbol? m = fieldsAndProperties[i];
                    sb.AppendLine($"sb.Append(\"{m.Name} = \");");
                    sb.AppendLine($"sb.Append({m.Name}.ToString());");

                    if (i != fieldsAndProperties.Count - 1)
                    {
                        sb.AppendLine("sb.Append(\", \");");
                    }
                }

                sb.AppendLine("return true;");
                printMembersBody = sb.ToString();
            }

            result = result.Replace("$$PRINT_MEMBERS_BODY$$", printMembersBody);
            return true;
        }

        /// <inheritdoc/>
        protected override (string attributeName, string attributeText) GetAttribute()
        {
            return ("GenerateToStringAttribute", attributeText);
        }

        /// <inheritdoc/>
        protected override IMethodSymbol[] GetExistingMembersToGenerate(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsObjectToStringOverride()).ToArray();
        }
    }
}
