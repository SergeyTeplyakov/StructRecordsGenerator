using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    [Ignore("Not supported by the infrastructure")]
    public class TestMultipleAttribute
    {
        [Test]
        public void StructEqualityAndToStringShouldNotCollide()
        {
            string code = @"
[StructGenerators.ToStringGenerator]
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    public readonly int X;
    public MyStruct(int x) {X = x;}
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Equals");
            output.Should().Contain("ToString");
        }
    }
}
