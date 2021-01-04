using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public class TestStructRecordGenerator
    {
        [Test]
        public void StructWithNoFieldsShouldCompile()
        {
            string code = @"
[StructGenerators.StructRecord]
public partial struct MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("private bool PrintMembers(StringBuilder");
        }

        [Test]
        public void WithMethodsGeneratedAllNonPrivateFieldsAndProperties()
        {
            string code = @"
namespace FooBar
{
    [StructGenerators.StructRecord]
    public partial struct MyStruct
    {
        private readonly string _NotIncluded;
        public readonly string _S;
        public string S {get;}
        public string S2 {get; private set;}
        public string S3 {get; init;}
        private string NotIncludedProp {get; init;}
        private string NotIncludedProp2 => string.Empty;
        private string NotIncludedProp3 { get => string.Empty;}
    }
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().NotContain("WithNotIncluded");
            output.Should().Contain("WithS(");
            output.Should().Contain("WithS2(");
            output.Should().Contain("WithS3(");
            output.Should().Contain("With_S(");
        }

        [Test]
        public void StructAlreadyHasToString()
        {
            string code = @"
        [StructGenerators.StructRecord]
        public partial struct MyStruct
        {
            private readonly string _s;
            public override string ToString() => string.Empty;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().NotContain("ToString()");
        }
        
        [Test]
        public void StructWith5Fields()
        {
            string code = @"
namespace X {
        [StructGenerators.StructRecord]
    public partial struct MyStruct
    {
        private readonly int _t1;
        private readonly int _t2;
        private readonly int _t3;
        private readonly int _t4;
        private readonly int _t5;
        private static string _staticS;

        public void FoOBar()
        {
            //this.ToString
        }
    }
}
        ";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("ToString()");
        }

        [Test]
        public void StructWithOneFieldAndStatic()
        {
            string code = @"
        [StructGenerators.StructRecord]
        public partial struct MyStruct
        {
            private readonly string _s;
            private static string _staticS;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Append((object)_s)");
            output.Should().NotContain("_staticS.ToString()");
        }

        [Test]
        public void GenericStructWithOneFieldAndStatic()
        {
            string code = @"
        [StructGenerators.StructRecord]
        public partial struct MyStruct<T1, T2, T3, T4, T5>
        {
            public readonly T1 _t1;
            public readonly T2 _t2;
            public readonly T3 _t3;
            public readonly T4 _t4;
            public readonly T5 _t5;
            private static string _staticS;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("ToString()");
            output.Should().Contain("_t1?.ToString()");
            output.Should().Contain("_t5?.ToString()");
            output.Should().NotContain("_staticS.ToString()");
            output.Should().NotContain("$$TYPE_NAME$$");
        }

        [Test]
        public void CloneMethodShouldBeGenerated()
        {
            string code = @"
        [StructGenerators.StructRecord]
        public partial struct MyStruct
        {
            public readonly int _t1;
            public readonly int _t2;
            public readonly int _t3;
            public readonly int _t4;
            private readonly int _t5;
            private static string _staticS;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Clone()");
        }

        [Test]
        public void TestExistingConstructor()
        {
            string code = @"
[StructGenerators.StructRecord]
public partial struct MyStruct
{
    public readonly int X;
    public readonly string S;
    public MyStruct(int x, string s) {X = x; S = s;}
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("WithX(");
        }
        
        [Test]
        public void TestExistingClone()
        {
            string code = @"
[StructGenerators.StructRecord]
public partial struct MyStruct
{
    public int X;
    public MyStruct Clone() => default;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructRecordGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("WithX(");
        }
    }
}
