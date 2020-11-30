using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructRecordGeneratorSample
{
    [StructGenerators.StructEquality]
    partial struct S1
    {
        private readonly int x1;
        private readonly int x2;

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
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
