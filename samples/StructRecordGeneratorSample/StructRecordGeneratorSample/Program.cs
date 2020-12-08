//using System;
//using System.Text;

using System;
using System.Text;

namespace StructRecordGeneratorSample
{
    [StructGenerators.StructEquality]
    public partial struct Point3
    {
        public double X { get; init; }
        public double Y { get; init; }
    }

    //[StructGenerators.StructEquality]
    //partial struct S1
    //{
    //    private readonly int x1;
    //    private int x3 => 42;
    //    private int x4 { get; set; }

    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode();
    //    }
    //}

    public struct S1
    {
        public double X { get; }
        public S1(double x) => (X) = x;
    }

    //public record X(int X) {

    //}

    //public class XY : X
    //{

    //}

    public struct S2
    {
        public byte B { get; }
        public double X { get; }
        public S2(double x) => (X, B) = (x, 0);
    }

    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        // The first line prints 'False',
    //        // because S1 is properly packed and the runtime uses
    //        // a bit-wise comparison for two instances.
    //        // And even though -0.0 is equals to +0.0 they do have
    //        // a different binary representation, so the result is false.
    //        Console.WriteLine(new S1(-0.0).Equals(new S1(+0.0))); // False

    //        // The next line prints 'True',
    //        // because the optimized version of ValueType.Equals
    //        // can't be used here, because S2 is not properly packed!
    //        Console.WriteLine(new S2(-0.0).Equals(new S2(+0.0))); // True
    //    }
    //}

    //    [StructGenerators.StructEquality]
    //    public partial struct F(int x, string y)
    //    { }

    //[StructGenerators.StructEquality]
    //public partial struct S
    //{
    //    private F f;
    //    private StringBuilder sb;
    //    public double D { get; }
    //    public S(double d) => (D, f, sb) = (d, new F(), null);
    //}



    /// <summary>
    /// Record P
    /// </summary>
    // warning CS1591: Missing XML comment for publicly visible type or member 'P.D'
    public record P(double D);



    class Program
    {

        //private static bool Foo<T>(T t1, T t2)
        //{
        //    // error CS0019: Operator '==' cannot be applied to operands of type 'T' and 'T'
        //    return (t1, t2) == (t1, t2);
        //}











        static void Main(string[] args)
        {
            Point3 p = default;
            Point3 p2 = default;
            bool b = p == p2;
            // The differences between Double.Equals and Double==
            //Console.WriteLine(double.NaN.Equals(double.NaN)); // True
            //Console.WriteLine(double.NaN == double.NaN); // False

            //// The same is true for tuples!
            //Console.WriteLine((double.NaN, 1).Equals((double.NaN, 1))); // True
            //Console.WriteLine((double.NaN, 1) == (double.NaN, 1)); // False

            //// But records in C# 9 behave differently!
            //Console.WriteLine(new P(double.NaN).Equals(new P(double.NaN))); // True
            //Console.WriteLine(new P(double.NaN) == new P(double.NaN)); // True
        }


    }

}
