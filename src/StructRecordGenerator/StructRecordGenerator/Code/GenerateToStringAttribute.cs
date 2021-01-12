using System;

namespace StructGenerators
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateToStringAttribute : Attribute
    {
        /// <summary>
        /// If true, the type name will be printed as part of ToString result.
        /// </summary>
        public bool PrintTypeName { get; set; } = false;

        /// <summary>
        /// The max length of a final string representation.
        /// </summary>
        public int MaxStringLength { get; set; } = 1024;
    }

    /// <summary>
    /// Defines a ToString behavior for collection fields and properties.
    /// </summary>
    internal enum CollectionsBehavior
    {
        /// <summary>
        /// Only the type name and the count (if available) is printed for members of a collection type.
        /// </summary>
        PrintTypeNameAndCount,
        
        /// <summary>
        /// The content of the collection is printed.
        /// </summary>
        PrintContent,
    }

    /// <summary>
    /// Controls the behavior of ToString method for a member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class ToStringBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Defines ToString behavior for collection fields and properties.
        /// </summary>
        public CollectionsBehavior CollectionsBehavior { get; set; } = CollectionsBehavior.PrintTypeNameAndCount;
        
        /// <summary>
        /// A number of elements printed for a collection member (only used when <see cref="CollectionsBehavior"/> is <see cref="StructGenerators.CollectionsBehavior.PrintContent"/>.
        /// </summary>
        public int CollectionCountLimit { get; set; } = 100;

        /// <summary>
        /// If true, the member won't be printed as part inside ToString implementation.
        /// </summary>
        public bool Skip { get; set; }
    }
}
