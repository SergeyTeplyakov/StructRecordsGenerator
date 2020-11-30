namespace StructRecordGeneratorSample
{
    [StructGenerators.StructEquality]
    partial struct S1
    {
        private readonly int x1;
        private int x3 => 42;
        private int x4 { get; init; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            S1 s1 = default;
            S1 s2 = default;
            s1.Equals(s2);

            bool b = s1 == s2;
        }
    }
}
