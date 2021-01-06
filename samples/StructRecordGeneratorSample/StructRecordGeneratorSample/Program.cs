//using System;
//using System.Text;

using System;
using StructGenerators;
#nullable disable
// assembly level attribute
namespace StructRecordGeneratorSample
{

    [GenerateToString(PrintTypeName = true, MaxStringLength = 5000)]
    public partial class CustomClass<T>
    {
        // The record prints S = System.String[]
        // Printing the content here by default instead!
        [ToStringBehavior(CollectionsBehavior = CollectionsBehavior.PrintContent)]
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

    class Program
    {

        static void Main(string[] args)
        {
            //StructGenerators.ToStringGenerationHelper
            var cr = new CustomClass<int>();
            Console.WriteLine(cr.ToString());
        }
    }
}
