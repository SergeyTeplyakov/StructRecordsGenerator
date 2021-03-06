﻿using FluentAssertions;

using NUnit.Framework;

using StructRecordGenerators.Generators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public partial class TestToStringGenerator
    {
        [Test]
        public void StructWithNoFields()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);
            
            output.Should().Contain(@"sb.Append(""MyStruct "");");
            output.Should().Contain("private bool PrintMembers(StringBuilder");
        }

        [Test]
        public void ClassWithNoFields()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial class MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("protected virtual bool PrintMembers(StringBuilder");
        }

        [Test]
        public void SealedClassWithNoFields()
        {
            string code = @"
[StructGenerators.GenerateToString]
public sealed partial class MyStruct
{
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("private bool PrintMembers(StringBuilder");
        }

        [Test]
        public void StructWithOneField()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
    private readonly string _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("sb.Append((object)_s)");
        }

        [Test]
        public void ReferenceTypeFieldShouldHaveCast()
        {
            string code = @"
[StructGenerators.GenerateToString]
public partial struct MyStruct
{
    private readonly string _s;
}
";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("(object)_s");
        }

        [Test]
        public void StructAlreadyHasToString()
        {
            // TODO: this test can do a better job and detect exactly what is going on.
            // This should be fine: the generated code should just skip it.
            string code = @"
        [StructGenerators.GenerateToString]
        public partial struct MyStruct
        {
            private readonly string _s;
            public override string ToString() => string.Empty;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().NotContain("ToString()");
        }
        
        [Test]
        public void StructWith5Fields()
        {
            string code = @"
namespace X {
        [StructGenerators.GenerateToString]
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

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("ToString()");
        }

        [Test]
        public void StructWithOneFieldAndStatic()
        {
            string code = @"
        [StructGenerators.GenerateToString]
        public partial struct MyStruct
        {
            private readonly string _s;
            private static string _staticS;
        }
        ";

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("Append((object)_s)");
            output.Should().NotContain("Append((object)_staticS)");
        }

        [Test]
        public void GenericStructWithOneFieldAndStatic()
        {
            string code = @"
        [StructGenerators.GenerateToString]
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

            var generatorTestHelper = new GeneratorTestHelper<ToStringGenerator>();
            var output = generatorTestHelper.GetGeneratedOutput(code);

            output.Should().Contain("ToString()");
            // No casts for generics! This will cause boxing if T1 is value type, I guess.
            output.Should().Contain("Append(_t1?.ToString())");
            output.Should().Contain("Append(_t5?.ToString())");
            output.Should().NotContain("Append(_staticS)");
        }
    }
}
