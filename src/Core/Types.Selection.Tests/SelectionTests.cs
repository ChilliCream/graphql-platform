using System.Linq;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Types
{
    public class SelectionTests
    {
        [Fact]
        public void Execute_Selection_Fragment()
        {
            // arrange
            Foo[] foos = new[]
            {
                Foo.Create("aa",1),
                Foo.Create("bb",2),
            };
            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(foos)
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { ...test  } } fragment test on Foo { bar baz}");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Bar);
                    Assert.Equal(1, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(2, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                });
        }

        [Fact]
        public void Execute_Selection_Scalar()
        {
            // arrange
            Foo[] foos = new[]
            {
                Foo.Create("aa",1),
                Foo.Create("bb",2),
            };
            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(foos)
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { bar } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                });
        }

        [Fact]
        public void Execute_Selection_MultipleScalar()
        {
            // arrange
            Foo[] foos = new[]
            {
                Foo.Create("aa",1),
                Foo.Create("bb",2),
            };
            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(foos)
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { bar baz } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Bar);
                    Assert.Equal(1, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(2, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                });
        }

        [Fact]
        public void Execute_Selection_Object()
        {
            // arrange
            Foo[] foos = new[]
            {
                Foo.Create("aa",1),
                Foo.Create("bb",2),
            };
            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(foos)
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { nested { bar } } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.NotNull(x.Nested);
                    Assert.Equal("nestedaa", x.Nested.Bar);
                    Assert.Equal(2, x.Nested.Baz);
                    Assert.Null(x.NestedCollection);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.NotNull(x.Nested);
                    Assert.Equal("nestedbb", x.Nested.Bar);
                    Assert.Equal(4, x.Nested.Baz);
                    Assert.Null(x.NestedCollection);
                });
        }
        [Fact]
        public void Execute_Selection_Collection()
        {
            // arrange
            Foo[] foos = new[]
            {
                Foo.Create("aa",1),
                Foo.Create("bb",2),
            };
            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(foos)
                        .Use(next => async ctx =>
                        {
                            await next(ctx);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { nestedCollection { bar } } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Single(x.NestedCollection);
                    Assert.Equal("nestedcollectionaa", x.NestedCollection[0].Bar);
                    Assert.Equal(3, x.NestedCollection[0].Baz);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Single(x.NestedCollection);
                    Assert.Equal("nestedcollectionbb", x.NestedCollection[0].Bar);
                    Assert.Equal(6, x.NestedCollection[0].Baz);
                });
        }
        private class Query
        {
            public IQueryable<Foo> Foos { get; }
        }

        private class Foo
        {
            public string Bar { get; set; }

            public int Baz { get; set; }

            public NestedFoo Nested { get; set; }
            public NestedFoo[] NestedCollection { get; set; }


            public static Foo Create(string bar, int baz)
            {
                return new Foo
                {
                    Bar = bar,
                    Baz = baz,
                    Nested = new NestedFoo()
                    {
                        Bar = "nested" + bar,
                        Baz = baz * 2
                    },
                    NestedCollection = new NestedFoo[]
                       {
                        new NestedFoo()
                        {
                            Bar = "nestedcollection" + bar,
                            Baz = baz * 3
                        },
                       }
                };
            }
        }

        private class NestedFoo
        {
            public string Bar { get; set; }

            public int Baz { get; set; }
        }
    }


}
