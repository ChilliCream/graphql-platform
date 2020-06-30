using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;
using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionBooleanTests
        : ExpressionVisitorConventionTestBase
    {
        [Fact]
        public void Override_BooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

            AssertOperation<Foo>(
                value,
                FilterKind.Boolean,
                FilterOperationKind.Equals,
                BooleanOperationHandlers.Equals);
        }

        [Fact]
        public void Override_BooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not",
                    new BooleanValueNode(true)));

            AssertOperation<Foo>(
                value,
                FilterKind.Boolean,
                FilterOperationKind.NotEquals,
                BooleanOperationHandlers.NotEquals);
        }

        public class Foo
        {
            public bool Bar { get; set; }
        }
    }
}
