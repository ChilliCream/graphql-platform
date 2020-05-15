using System;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionObjectTests
        : ExpressionVisitorConventionTestBase
    {
        [Fact]
        public void Override_ObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            AssertEnterAndLeave<Foo>(
                value,
                FilterKind.Object,
                ObjectFieldHandler.Enter,
                ObjectFieldHandler.Leave);
        }

        public class Foo
        {
            public FooNested FooNested { get; set; }
        }
        public class FooNested
        {
            public string Bar { get; set; }
        }
        public class Recursive
        {
            public Recursive Nested { get; set; }
            public string Bar { get; set; }
        }
    }
}
