using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;

using NUnit.Framework;
using StructGenerators;

namespace StructRecordGenerators.Tests
{
    [TestFixture]
    public class ToStringGenerationHelperTests
    {
        private static readonly int?[] ArrayOfNullableInt = new int?[] {1, null, 2};
        
        [Test]
        public void PrintContentForNullableIntArray()
        {
            var sb = new StringBuilder();
            sb.PrintCollection(ArrayOfNullableInt, "x", CollectionsBehavior.PrintContent, limit: 10);
            Console.WriteLine(sb.ToString());

            sb.ToString().Should().Be("x (Count: 3) = [1, , 2]");
        }
        
        [Test]
        public void PrintContentForNullableIntArray_WithLimit()
        {
            var sb = new StringBuilder();
            sb.PrintCollection(ArrayOfNullableInt, "x", CollectionsBehavior.PrintContent, limit: 2);
            Console.WriteLine(sb.ToString());

            sb.ToString().Should().Be("x (Count: 3, Limit: 2) = [1, ]");
        }
        
        [Test]
        public void PrintContentForNullableIntArray_AsEnumerable()
        {
            var sb = new StringBuilder();
            sb.PrintCollection((IEnumerable<int?>)ArrayOfNullableInt, "x", CollectionsBehavior.PrintContent, limit: 2);
            Console.WriteLine(sb.ToString());

            sb.ToString().Should().Be("x (Count: 3, Limit: 2) = [1, ]");
        }
        
        [Test]
        public void PrintContentForNullableIntArray_PrintCount()
        {
            var sb = new StringBuilder();
            sb.PrintCollection(ArrayOfNullableInt, "x", CollectionsBehavior.PrintTypeNameAndCount, limit: 2);
            Console.WriteLine(sb.ToString());

            sb.ToString().Should().Be("x (Count: 3) = System.Nullable`1[System.Int32][]");
        }
        
        [Test]
        public void PrintContentForEnumerable_TypeNameAndCount()
        {
            var sb = new StringBuilder();
            sb.PrintCollection(Range(10), "x", CollectionsBehavior.PrintTypeNameAndCount, limit: 5);
            Console.WriteLine(sb.ToString());

            sb.ToString().Should().Contain("x = StructRecordGenerators.Tests.ToStringGenerationHelperTests+<Range>d__");
        }

        [Test]
        public void PrintContentForEnumerable_Content()
        {
            var sb = new StringBuilder();
            sb.PrintCollection(Range(10), "x", CollectionsBehavior.PrintContent, limit: 5);
            Console.WriteLine(sb.ToString());
            sb.ToString().Should().Be("x (Limit: 5) = [0, 1, , 2, 3]");
        }

        private static IEnumerable<int?> Range(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i == 2)
                {
                    yield return null;
                }

                yield return i;
            }
        }
    }
}
