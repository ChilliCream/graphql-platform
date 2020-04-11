using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionArrayTests
        : ExpressionVisitorConventionTestBase
    {
        [Fact]
        public void Override_ArraySomeStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("element",
                            new StringValueNode("a")))));

            AssertEnterAndLeave<FooSimple>(
                value,
                FilterKind.Array,
                ArrayFieldHandler.Enter,
                ArrayFieldHandler.Leave);
        }

        [Fact]
        public void Override_ArrayAnyStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_any",
                            new BooleanValueNode(true)
                        )
            );

            AssertOperation<FooSimple>(
                value,
                FilterKind.Array,
                FilterOperationKind.ArrayAny,
                ArrayOperationHandler.ArrayAny);
        }

        [Fact]
        public void Override_ArraySomeStringEqualWithNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("element",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            AssertEnterAndLeave<FooSimple>(
                value,
                FilterKind.Array,
                ArrayFieldHandler.Enter,
                ArrayFieldHandler.Leave);
        }

        [Fact]
        public void Override_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            AssertEnterAndLeave<Foo>(
                value,
                FilterKind.Array,
                ArrayFieldHandler.Enter,
                ArrayFieldHandler.Leave);
        }

        [Fact]
        public void Override_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(true)
                )
            );

            AssertOperation<Foo>(
                value,
                FilterKind.Array,
                FilterOperationKind.ArrayAny,
                ArrayOperationHandler.ArrayAny);
        }

        public class Foo
        {
            public IEnumerable<FooNested> FooNested { get; set; }
        }

        public class FooSimple
        {
            public IEnumerable<string> Bar { get; set; }
        }

        public class FooNested
        {
            public string Bar { get; set; }
        }

    }
}
