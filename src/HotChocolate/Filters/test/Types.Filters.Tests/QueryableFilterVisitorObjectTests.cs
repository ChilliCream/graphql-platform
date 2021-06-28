using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    [Obsolete]
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

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<Foo, bool> func = filterContext.CreateFilter<Foo>().Compile();

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

            FooFilterType fooType = CreateType(new FooFilterType());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(Foo),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<Foo, bool> func = filterContext.CreateFilter<Foo>().Compile();

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

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(EvenDeeper),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<EvenDeeper, bool> func = filterContext.CreateFilter<EvenDeeper>().Compile();

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

            FilterInputType<Recursive> fooType = CreateType(new FilterInputType<Recursive>());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(Recursive),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<Recursive, bool> func = filterContext.CreateFilter<Recursive>().Compile();


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

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(EvenDeeper),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<EvenDeeper, bool> func = filterContext.CreateFilter<EvenDeeper>().Compile();

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
         * As multiple hanlders for a single property can exists, it makes sense to test mutliple porperty filterContexting too
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

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());

            // act
            var filterContext = new QueryableFilterVisitorContext(
                fooType,
                typeof(EvenDeeper),
                DefaultTypeConverter.Default,
                true);
            QueryableFilterVisitor.Default.Visit(value, filterContext);
            Func<EvenDeeper, bool> func = filterContext.CreateFilter<EvenDeeper>().Compile();

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
