using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public class TestToStringGeneratorForRecords
    {
        [Test]
        public void Record()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial record MyRecord
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            
            output.Should().Contain(@"sb.Append(""MyRecord "");");
            output.Should().Contain("virtual bool PrintMembers(StringBuilder sb)");
        }
        
        [Test]
        public void RecordWithPrimaryConstructor()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial record MyRecord(int Property1)
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            
            output.Should().Contain(@"sb.Append(""MyRecord "");");
            output.Should().Contain("virtual bool PrintMembers(StringBuilder sb)");
            output.Should().Contain("Property1");
            output.Should().NotContain("EqualityContract");
        }
    }
}
