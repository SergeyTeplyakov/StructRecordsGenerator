using System.Linq;
using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public partial class TestToStringGenerator
    {
        [Test]
        public void PrintTypeNameIsFalse()
        {
            string code = @"
[StructGenerators.GenerateToString(PrintTypeName = false)]
public partial struct MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain(@"// sb.Append(""MyStruct "");");
        }

        [Test]
        public void SkipFieldsAndProperties()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
    [StructGenerators.ToStringImpl(Skip = true)]
    private readonly string _s;
    
    [StructGenerators.ToStringImpl(Skip = true)]
    public string S => _s;

    public string Y => _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().NotContain("Append((object)_s)");
            output.Should().NotContain("Append((object)S)");
            output.Should().Contain("Append((object)Y)");
        }

        [Test]
        public void LegacyCollectionToStringBehavior()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
    [StructGenerators.ToStringImpl(PrintTypeNameForCollections = true)]
    private readonly string[] _s;
    
    public string[] S => _s;

    [StructGenerators.ToStringImpl(PrintTypeNameForCollections = false)]
    public int[] S2 => null;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            output.Should().Contain("Append((object)_s)");
            output.Should().Contain("S.Take(100).Select(");
            output.Should().Contain("S2.Take(100).Select(");
        }

        [Test]
        public void RespectTheLimits()
        {
            string code = @"
[StructGenerators.GenerateToString(MaxStringLength = 142)]
public partial struct MyStruct
{
    [StructGenerators.ToStringImpl(MaxElementCount = 99)]
    private readonly string[] _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            output.Should().Contain("(limit: 99)");
            output.Should().Contain("142");
        }
    }
}
