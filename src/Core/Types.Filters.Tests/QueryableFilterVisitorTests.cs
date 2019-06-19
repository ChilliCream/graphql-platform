using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorTests
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
            }
        }
    }
}
