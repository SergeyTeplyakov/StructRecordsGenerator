using FluentAssertions;
using NUnit.Framework;

namespace StructRecordGenerator.Tests
{
    [TestFixture]
    public class TestStructEqualityGenerator_Diagnostics
    {
        [Test]
        public void WarnOnNonPartialStruct()
        {
            string code = @"
[StructGenerators.StructEquality]
public struct MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructMustBePartialDiagnosticDiagnosticId);
        }

        [Test]
        public void WarnOnGetHashCode()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    public override int GetHashCode() => 42;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructAlreadyImplementsEqualityMemberId);
        }

        [Test]
        public void WarnOnEquals()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    public override bool Equals(object other) => true;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructAlreadyImplementsEqualityMemberId);
        }
        
        [Test]
        public void WarnOnEqualsFromInterface()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct : System.IEquatable<MyStruct>
{
    public bool Equals(MyStruct other) => true;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructAlreadyImplementsEqualityMemberId);
        }

        [Test]
        public void WarnOnOperatorEquals()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    public static bool operator ==(MyStruct left, MyStruct right) => true;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructAlreadyImplementsEqualityMemberId);
        }

        [Test]
        public void WarnOnOperatorNotEquals()
        {
            string code = @"
[StructGenerators.StructEquality]
public partial struct MyStruct
{
    public static bool operator !=(MyStruct left, MyStruct right) => true;
}
";

            var generatorTestHelper = new GeneratorTestHelper<StructEqualityGenerator>();
            var diagnostics = generatorTestHelper.GetGeneratedDiagnostics(code);

            diagnostics.Should().Contain(d => d.Id == StructGeneratorAnalyzer.StructAlreadyImplementsEqualityMemberId);
        }
    }
}
