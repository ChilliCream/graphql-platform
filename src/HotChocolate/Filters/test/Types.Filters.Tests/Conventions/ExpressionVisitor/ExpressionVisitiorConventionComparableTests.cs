using System;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ExpressionVisitorConventionComparableTests
       : ExpressionVisitorConventionTestBase
    {
        [Fact]
        public void Override_ShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.Equals,
                ComparableOperationHandlers.Equals);
        }

        [Fact]
        public void Override_ShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotEquals,
                ComparableOperationHandlers.NotEquals);
        }

        [Fact]
        public void Override_ShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.GreaterThan,
                ComparableOperationHandlers.GreaterThan);
        }

        [Fact]
        public void Override_ShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotGreaterThan,
                ComparableOperationHandlers.NotGreaterThan);
        }

        [Fact]
        public void Override_ShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.GreaterThanOrEquals,
                ComparableOperationHandlers.GreaterThanOrEquals);
        }

        [Fact]
        public void Override_ShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotGreaterThanOrEquals,
                ComparableOperationHandlers.NotGreaterThanOrEquals);
        }

        [Fact]
        public void Override_ShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.LowerThan,
                ComparableOperationHandlers.LowerThan);
        }

        [Fact]
        public void Override_ShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotLowerThan,
                ComparableOperationHandlers.NotLowerThan);
        }

        [Fact]
        public void Override_ShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.LowerThanOrEquals,
                ComparableOperationHandlers.LowerThanOrEquals);
        }

        [Fact]
        public void Override_ShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotLowerThanOrEquals,
                ComparableOperationHandlers.NotLowerThanOrEquals);
        }

        [Fact]
        public void Override_ShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.In,
                ComparableOperationHandlers.In);
        }

        [Fact]
        public void Override_ShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            AssertOperation<Foo>(
                value,
                FilterKind.Comparable,
                FilterOperationKind.NotIn,
                ComparableOperationHandlers.NotIn);
        }

        public class Foo
        {
            public short BarShort { get; set; }
            public int BarInt { get; set; }
            public long BarLong { get; set; }
            public float BarFloat { get; set; }
            public double BarDouble { get; set; }
            public decimal BarDecimal { get; set; }
        }
    }
}
