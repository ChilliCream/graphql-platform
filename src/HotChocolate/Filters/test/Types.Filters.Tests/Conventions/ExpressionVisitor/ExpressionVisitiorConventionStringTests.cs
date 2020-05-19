using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionStringTests
        : ExpressionVisitorConventionTestBase
    {
        [Fact]
        public void Override_StringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.Equals,
                StringOperationHandlers.Equals);
        }

        [Fact]
        public void Override_StringNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.NotEquals,
                StringOperationHandlers.NotEquals);
        }

        [Fact]
        public void Override_StringIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_in",
                    new ListValueNode(new[]
                    {
                        new StringValueNode("a"),
                        new StringValueNode("c")
                    })));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.In,
                StringOperationHandlers.In);
        }

        [Fact]
        public void Override_StringNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_in",
                    new ListValueNode(new[]
                    {
                        new StringValueNode("a"),
                        new StringValueNode("c")
                    })));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.NotIn,
                StringOperationHandlers.NotIn);
        }

        [Fact]
        public void Override_StringContains_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_contains",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.Contains,
                StringOperationHandlers.Contains);
        }

        [Fact]
        public void Override_StringNoContains_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_contains",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.NotContains,
                StringOperationHandlers.NotContains);
        }

        [Fact]
        public void Override_StringStartsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_starts_with",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.StartsWith,
                StringOperationHandlers.StartsWith);
        }

        [Fact]
        public void Override_StringNotStartsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_starts_with",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.NotStartsWith,
                StringOperationHandlers.NotStartsWith);
        }

        [Fact]
        public void Override_StringEndsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_ends_with",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.EndsWith,
                StringOperationHandlers.EndsWith);
        }

        [Fact]
        public void Override_StringNotEndsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_ends_with",
                    new StringValueNode("a")));

            AssertOperation<Foo>(
                value,
                FilterKind.String,
                FilterOperationKind.NotEndsWith,
                StringOperationHandlers.NotEndsWith);
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
