using System;
using HotChocolate.Language;
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
                new ObjectFieldNode(
                    "bar",
                    new BooleanValueNode(true)));

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo {Bar = true};
            Assert.True(func(a));

            var b = new Foo {Bar = false};
            Assert.False(func(b));
        }

        [Fact]
        public void Create_BooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new BooleanValueNode(false)));

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo {Bar = false};
            Assert.True(func(a));

            var b = new Foo {Bar = true};
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableBooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new BooleanValueNode(true)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooNullableType,
                typeof(FooNullable),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable {Bar = true};
            Assert.True(func(a));

            var b = new FooNullable {Bar = false};
            Assert.False(func(b));

            var c = new FooNullable {Bar = null};
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode(
                    "bar",
                    new BooleanValueNode(false)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitorContext(
                fooNullableType,
                typeof(FooNullable),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable {Bar = false};
            Assert.True(func(a));

            var b = new FooNullable {Bar = true};
            Assert.False(func(b));

            var c = new FooNullable {Bar = null};
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
                    .AllowEquals()
                    .And()
                    .AllowNotEquals();
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals()
                    .And()
                    .AllowNotEquals();
            }
        }
    }
}
