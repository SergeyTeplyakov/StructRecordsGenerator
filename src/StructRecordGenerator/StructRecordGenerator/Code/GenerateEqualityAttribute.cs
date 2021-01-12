using System;

namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateEqualityAttribute : Attribute
    {
    }
}
