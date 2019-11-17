using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorArrayTests
        : TypeTestBase
    {
        [Fact]
        public void Create_ArraySomeStringEqual_Expression()
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

            var fooType = CreateType(new FooSimpleFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(FooSimple),
                TypeConversion.Default);
            value.Accept(filter);
            Func<FooSimple, bool> func = filter.CreateFilter<FooSimple>().Compile();

            // assert
            var a = new FooSimple { Bar = new[] { "c", "d", "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", "d", "b" } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArraySomeStringEqualWithNull_Expression()
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

            var fooType = CreateType(new FooSimpleFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(FooSimple),
                TypeConversion.Default);
            value.Accept(filter);
            Func<FooSimple, bool> func = filter.CreateFilter<FooSimple>().Compile();

            // assert
            var a = new FooSimple { Bar = new[] { "c", null, "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", null, "b" } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArraySomeObjectStringEqualWithNull_Expression()
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

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" },
                    null, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.True(func(a));

            var b = new Foo 
            {
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" },
                    null, 
                    new FooNested { Bar = "b" } 
                } 
            };
            Assert.False(func(b));
        }
        [Fact]
        public void Create_ArraySomeObjectStringEqual_Expression()
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

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.True(func(a));

            var b = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "b" } 
                } 
            };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArrayNoneObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_none",
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
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.False(func(a));

            var b = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "b" } 
                } 
            };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_all",
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
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "a" }, 
                    new FooNested { Bar = "a" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.True(func(a));

            var b = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "a" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.False(func(b));

            var c = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "a" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "b" } 
                } 
            };
            Assert.False(func(c));

            var d = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "b" } 
                } 
            };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(true)
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
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.True(func(a));

            var b = new Foo { FooNested = new FooNested[] { } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArrayNotAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(false)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Foo),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo 
            { 
                FooNested = new[] 
                { 
                    new FooNested { Bar = "c" }, 
                    new FooNested { Bar = "d" }, 
                    new FooNested { Bar = "a" } 
                } 
            };
            Assert.False(func(a));

            var b = new Foo { FooNested = new FooNested[] { } };
            Assert.True(func(b));
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

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.List(t => t.FooNested).BindImplicitly();
            }
        }

        public class FooSimpleFilterType
            : FilterInputType<FooSimple>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooSimple> descriptor)
            {
                descriptor.List(t => t.Bar).BindImplicitly();
            }
        }
    }
}
