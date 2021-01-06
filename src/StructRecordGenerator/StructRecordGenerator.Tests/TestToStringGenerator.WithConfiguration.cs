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
    [StructGenerators.ToStringBehavior(Skip = true)]
    private readonly string _s;
    
    [StructGenerators.ToStringBehavior(Skip = true)]
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
    private readonly string[] _s;
    
    public string[] S => _s;

    [StructGenerators.ToStringBehavior(CollectionsBehavior = StructGenerators.CollectionsBehavior.PrintContent)]
    public int[] S2 => null;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            output.Should().Contain("behavior: CollectionsBehavior.PrintContent");
            output.Should().Contain("limit: 100");
        }
        
        [Test]
        public void SkipIsRespected()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
    [StructGenerators.ToStringBehavior(Skip = true)]
    public int[] S2 => null;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            output.Should().NotContain("PrintCollection");
        }

        [Test]
        public void RespectTheLimits()
        {
            string code = @"
[StructGenerators.GenerateToString(MaxStringLength = 142)]
public partial struct MyStruct
{
    [StructGenerators.ToStringBehavior(CollectionCountLimit = 99)]
    private readonly string[] _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            output.Should().Contain(", limit: 99");
            output.Should().Contain("142");
        }
    }
}
