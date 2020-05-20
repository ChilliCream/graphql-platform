using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Expressions;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextBooleanTests
        : TypeTestBase
    {
        [Fact]
        public void Create_BooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<Foo, bool> func = filter.CreateOrAssert<Foo>().Compile();

            // assert
            var a = new Foo { Bar = true };
            Assert.True(func(a));

            var b = new Foo { Bar = false };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_BooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<Foo, bool> func = filter.CreateOrAssert<Foo>().Compile();

            // assert
            var a = new Foo { Bar = false };
            Assert.True(func(a));

            var b = new Foo { Bar = true };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableBooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<FooNullable, bool> func = filter.CreateOrAssert<FooNullable>().Compile();

            // assert
            var a = new FooNullable { Bar = true };
            Assert.True(func(a));

            var b = new FooNullable { Bar = false };
            Assert.False(func(b));

            var c = new FooNullable { Bar = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default,
                true);
            FilterVisitor<Expression>.Default.Visit(value, filter);
            Func<FooNullable, bool> func = filter.CreateOrAssert<FooNullable>().Compile();

            // assert
            var a = new FooNullable { Bar = false };
            Assert.True(func(a));

            var b = new FooNullable { Bar = true };
            Assert.False(func(b));

            var c = new FooNullable { Bar = null };
            Assert.False(func(c));
        }

        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals().And().AllowNotEquals();
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals().And().AllowNotEquals();
            }
        }
    }
}
