using Microsoft.CodeAnalysis;

using System;
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
        /// If > 0 then the string representation of a member will be trimmed.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// If true, the member won't be printed as part inside ToString implementation.
        /// </summary>
        public bool Skip { get; set; }
    }

    public class FooBar
    {
        [ToStringImpl(Limit = 10_000, LegacyCollectionsBehavior = true)]
        public int X { get; set; }
        
        [ToStringImpl(Skip = true)]
        public int Y { get; set; }
    }

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
        /// <summary>
        /// If true, the type name will be printed as part of ToString result.
        /// </summary>
        public bool PrintTypeName { get; set; } = true;
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
        public bool LegacyCollectionsBehavior { get; set; }

        /// <summary>
        /// If > 0 then the string representation of a member will be trimmed.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// If true, the member won't be printed as part inside ToString implementation.
        /// </summary>
        public bool Skip { get; set; }
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
    
    $$PRINT_BOODY_TYPE_NAME_PREFIX$$sb.Append(""$$TYPE_NAME$$"");

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

        private bool NeedToPrintTypeName(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
        {
            var attribute = typeSymbol.TryGetAttribute(attributeSymbol)!;
            TypedConstant? typeConstraint = null;
            
            attribute.NamedArguments.FirstOrDefault(a =>
            {
                var result = a.Key == "PrintTypeName";
                if (result)
                {
                    typeConstraint = a.Value;
                    return true;
                }

                return false;
            });
            
            if (typeConstraint is not null && typeConstraint.Value.Value is false)
            {
                return false;
            }

            return true;
        }

        public string GenerateBody(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol)
        {
            string modifier = typeSymbol.IsValueType || typeSymbol.IsSealed ? "private" : "protected virtual";

            string typeNamePrintPrefix = NeedToPrintTypeName(typeSymbol, attributeSymbol) ? string.Empty : "// ";

            var body = _bodyTemplate
                .Replace($"$$TYPE_NAME$$", typeSymbol.Name)
                .Replace("$$MODIFIER$$", modifier)
                .Replace("$$PRINT_BOODY_TYPE_NAME_PREFIX$$", typeNamePrintPrefix);

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
        protected override string GenerateClassWithNewMembers(INamedTypeSymbol typeSymbol, Compilation compilation, INamedTypeSymbol attributeSymbol)
        {
            string classOrStruct = typeSymbol.IsValueType ? "struct" : "class";
            var body = GenerateBody(typeSymbol, attributeSymbol);

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
