//using System;
//using System.Text;

using System;
using StructGenerators;
//#nullable disable
// assembly level attribute
namespace StructRecordGeneratorSample
{
    //[StructGenerators.StructRecord]
    //[StructGenerators.GenerateToString]

    //[StructEquality]

    /// <summary>
    /// sdfs
    /// </summary>
    [StructGenerators.GenerateToString]
    public readonly partial struct ContentHashWithLastAccessTime
    {
        /// <nodoc />
        public ContentHashWithLastAccessTime(ContentHash contentHash, DateTime lastAccessTime)
        {
            Hash = contentHash;
            LastAccessTime = lastAccessTime;
        }

        /// <summary>
        ///     Gets the content hash member.
        /// </summary>
        public ContentHash Hash { get; }

        /// <summary>
        ///     Gets the last time the content was accessed.
        /// </summary>
        public DateTime LastAccessTime { get; }

        public static void FooBar(ContentHashWithLastAccessTime e, ContentHashWithLastAccessTime e2)
        {
            //bool bv = e == e2;

            var s = e.ToString();
        }
    }

    public class ContentHash
    {
    }

    [GenerateToString(PrintTypeName = true, MaxStringLength = 5000)]
    public partial class CustomClass<T>
    {
        // The record prints S = System.String[]
        // Printing the content here by default instead!
        [ToStringBehavior(CollectionsBehavior = CollectionsBehavior.PrintTypeNameAndCount, CollectionCountLimit = 5)]
        // Just a count by default.
        // IEnumerable<Type> for IEnumerable (based on the runtime type, not based on compile-time type).
        // Configure the separator.
        public string[] S = new[] { "1", null, "2" };

        public string[] S2 => S;

        // No boxing allocation for Value property
        // in the generated ToString code
        [ToStringBehavior]
        public T Value { get; set; }
    }

    
    [StructGenerators.GenerateToString]
    //[GenerateStructEquality]
    [StructRecord]
    public partial struct ClassWithAShortName
    {

    }

    
    [GenerateToString]
    public partial class ClassWithAVeryLongNameLikeAReallyReallyLongOne
    {

    }

    class Program
    {

        static void Main(string[] args)
        {
            // The generated file name is 254 characters long!:
            //C:\Users\seteplia\AppData\Local\Temp\VisualStudioSourceGeneratedDocuments\a4d7f8c8-8038-4522-9c15-b11d61b996bf\StructRecordGenerator\
            // StructRecordGenerators.Generators.ToStringGenerator\StructRecordGeneratorSample.ClassWithAShortName_ToStringGenerator.cs
            var o1 = new ClassWithAShortName();
            Console.WriteLine(o1.ToString());
            
            // The generated file name exceeds 260 and GoToDefinition doesn't work any more!
            var o2 = new ClassWithAVeryLongNameLikeAReallyReallyLongOne();
            Console.WriteLine(o2.ToString());
        }
    }
}
