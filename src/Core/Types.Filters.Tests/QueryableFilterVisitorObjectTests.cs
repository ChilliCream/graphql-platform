using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorObjectTests
        : TypeTestBase
    {
        [Fact]
        public void Create_ObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { FooNested = new FooNested { Bar = "a" } };
            Assert.True(func(a));

            var b = new Foo { FooNested = new FooNested { Bar = "b" } };
            Assert.False(func(b));
        }
        /*
        [Fact]
        public void Create_StringNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "a" }};
            Assert.False(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "b" }};
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_in",
                    new ListValueNode(new[]
                    {
                        new StringValueNode("a"),
                        new StringValueNode("c")
                    })));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "a" }};
            Assert.True(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "b" }};
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_in",
                    new ListValueNode(new[]
                    {
                        new StringValueNode("a"),
                        new StringValueNode("c")
                    })));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "a" }};
            Assert.False(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "b" }};
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringContains_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_contains",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "testatest" }};
            Assert.True(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "testbtest" }};
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNoContains_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_not_contains",
                    new StringValueNode("a")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "testatest" }};
            Assert.False(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "testbtest" }};
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
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "ab" }};
            Assert.True(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "ba" }};
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
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "ab" }};
            Assert.False(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "ba" }};
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
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "ab" }};
            Assert.False(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "ba" }};
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
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo{FooNested =new FooNested { Bar = "ab" }};
            Assert.True(func(a));

            var b = new Foo{FooNested =new FooNested { Bar = "ba" }};
            Assert.False(func(b));
        }
        */
        public class Foo
        {
            public FooNested FooNested { get; set; }
        }
        public class FooNested
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.FooNested).AllowObject(x => x.Filter(y => y.Bar));
            }
        }
    }
}
