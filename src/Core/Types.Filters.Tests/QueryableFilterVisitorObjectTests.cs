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

        [Fact]
        public void Create_ObjectStringEqualWithNull_Expression()
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
            Func<Foo, bool> func = filter.CreateFilterInMemory<Foo>().Compile();

            // assert
            var a = new Foo { FooNested = new FooNested { Bar = "a" } };
            Assert.True(func(a));

            Foo b = null;
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ObjectStringEqualDeep_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")
                            )
                        )
                    )
                )
            )
            );

            var fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(EvenDeeper),
                TypeConversion.Default);
            value.Accept(filter);
            Func<EvenDeeper, bool> func = filter.CreateFilter<EvenDeeper>().Compile();

            // assert
            var a = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } };
            Assert.True(func(a));

            var b = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "b" } } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ObjectStringEqualRecursive_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("nested",
                    new ObjectValueNode(
                    new ObjectFieldNode("nested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")
                            )
                        )
                    )
                )
            )
            );

            var fooType = CreateType(new FilterInputType<Recursive>());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(Recursive),
                TypeConversion.Default);
            value.Accept(filter);
            Func<Recursive, bool> func = filter.CreateFilter<Recursive>().Compile();


            var a = new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "a" } } };
            Assert.True(func(a));

            var b = new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "b" } } };
            Assert.False(func(b));

        }


        [Fact]
        public void Create_ObjectStringEqualNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")
                            )
                        )
                    )
                )
            )
            );

            var fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(EvenDeeper),
                TypeConversion.Default);
            value.Accept(filter);
            Func<EvenDeeper, bool> func = filter.CreateFilter<EvenDeeper>().Compile();

            // assert
            var a = new EvenDeeper { Foo = null };
            Assert.False(func(a));

            var b = new EvenDeeper { Foo = new Foo { FooNested = null } };
            Assert.False(func(b));

            var c = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } };
            Assert.False(func(c));

            var d = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } };
            Assert.True(func(d));

        }

        /**
         * As multiple hanlders for a single property can exists, it makes sense to test mutliple porperty filtering too
         * Just to see if the null checks are wrapped around the whole object and not just around the one of the expressions.
         * With the current visitor implementation this cannot be the case. Anyway, as code lifes is good to check twice. 
         * */
        [Fact]
        public void Create_ObjectStringEqualNullWithMultipleFilters_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")
                            ),

                            new ObjectFieldNode("bar_not",
                                new StringValueNode("c")
                            )
                        )
                    )
                )
            )
            );

            var fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType,
                typeof(EvenDeeper),
                TypeConversion.Default);
            value.Accept(filter);
            Func<EvenDeeper, bool> func = filter.CreateFilter<EvenDeeper>().Compile();

            // assert
            var a = new EvenDeeper { Foo = null };
            Assert.False(func(a));

            var b = new EvenDeeper { Foo = new Foo { FooNested = null } };
            Assert.False(func(b));

            var c = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } };
            Assert.False(func(c));

            var d = new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } };
            Assert.True(func(d));

        }

        public class EvenDeeper
        {
            public Foo Foo { get; set; }
        }

        public class Foo
        {
            public FooNested FooNested { get; set; }
        }
        public class FooNested
        {
            public string Bar { get; set; }
        }
        public class Recursive
        {
            public Recursive Nested { get; set; }
            public string Bar { get; set; }
        }


        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Object(t => t.FooNested).AllowObject(x => x.Filter(y => y.Bar));
            }
        }

        public class EvenDeeperFilterType
            : FilterInputType<EvenDeeper>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<EvenDeeper> descriptor)
            {
                descriptor.Object(t => t.Foo).AllowObject(x => x.Object(y => y.FooNested).AllowObject(z => z.Filter(z => z.Bar)));
            }
        }
    }
}
