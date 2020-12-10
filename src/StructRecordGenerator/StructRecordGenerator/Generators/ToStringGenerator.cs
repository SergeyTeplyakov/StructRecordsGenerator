using Microsoft.CodeAnalysis;

using System.Linq;
using System.Text;

namespace StructRecordGenerators.Generators
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
}";

        /// <nodoc />
        public ToStringGenerator()
            : base(GeneratedTargetTypeKinds.Class | GeneratedTargetTypeKinds.Struct)
        {

        }

        public string GenerateBody(INamedTypeSymbol typeSymbol)
        {
            string modifier = typeSymbol.IsValueType || typeSymbol.IsSealed ? "private" : "protected virtual";
            var body = _bodyTemplate
                .Replace($"$$TYPE_NAME$$", typeSymbol.Name)
                .Replace("$$MODIFIER$$", modifier);

            var fieldsAndProperties = typeSymbol.GetNonStaticFieldsAndProperties(includeAutoPropertiesOnly: false);

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

            body = body.Replace("$$PRINT_MEMBERS_BODY$$", printMembersBody);
            return body;
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
        protected override string GenerateClassWithNewMembers(INamedTypeSymbol typeSymbol, Compilation compilation)
        {
            string classOrStruct = typeSymbol.IsValueType ? "struct" : "class";
            var body = GenerateBody(typeSymbol);

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
