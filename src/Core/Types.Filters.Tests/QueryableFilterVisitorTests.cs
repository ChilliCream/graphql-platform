using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Create_StringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "a" };
            Assert.False(func(a));

            var b = new Foo { Bar = "b" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringStartsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_starts_with",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.True(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotStartsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_starts_with",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.False(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringEndsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_ends_with",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.False(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringNotEndsWith_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_ends_with",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.True(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.False(func(b));
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .BindExplicitly()
                    .AllowContains().And()
                    .AllowEquals();
            }
        }
    }
}
