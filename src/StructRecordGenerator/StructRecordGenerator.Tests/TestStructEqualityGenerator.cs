using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public class TestStructEqualityGenerator
    {
        [Test]
        public void StructWithNoFields()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().Contain("42");
        }

        [Test]
        public void StructWithOneField()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    private readonly string _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().Contain("_s");
            output.Should().NotContain("42");
        }

        [Test]
        public void StructAlreadyHasGetHashCode()
        {
            // TODO: this test can do a better job and detect exactly what is going on.
            // This should be fine: the generated code should just skip it.
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    private readonly string _s;
    public override int GetHashCode() => 42;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().NotContain("GetHashCode");
        }

        [Test]
        public void StructWithOneFieldAndStatic()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    private readonly  string _s;
    private static string _staticS;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().NotContain("42");
            output.Should().NotContain("_staticS");
        }
        
        [Test]
        public void GenericStructWithOneFieldAndStatic()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct<T1, T2, T3, T4, T5>
{
    private readonly T1 _t1;
    private readonly T2 _t2;
    private readonly T3 _t3;
    private readonly T4 _t4;
    private readonly T5 _t5;
    private static string _staticS;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().NotContain("42");
            output.Should().NotContain("_staticS");
        }

        [Test]
        public void StructWithDoubleAndCustomStruct()
        {
            // BTW, the C# compiler devs explicitly decided not to go with this pattern for records to avoid code bloat.
            string code = @"
public struct S1
{
     public static bool operator ==(S1 left, S1 right) => true;
    public static bool operator !=(S1 left, S1 right) => true;
}

public struct S2
{
}

[StructGenerators.StructEquality]
public partial struct MyStruct
{
    private readonly double _v;
    private readonly S1 _s1;
    private readonly S2 _s2;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("(left._v, left._s1) == (right._v, right._s1) && (left._s2).Equals((right._s2))");
        }

        [Test]
        public void StructWithProperties()
        {
            string code = @"
namespace FooBar
{
    [StructGenerators.StructEquality]
    public partial struct MyStruct
    {
        private string P1 {get;}
        public string P2 {get; private set;}
        public string P3 {get; init;}
        public int P4 => 42;
        private static string StaticProperty {get;}
    }
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().Contain("P1");
            output.Should().Contain("P2");
            output.Should().Contain("P3");
            output.Should().NotContain("P4");
            output.Should().NotContain("_staticS");
        }
    }
}
