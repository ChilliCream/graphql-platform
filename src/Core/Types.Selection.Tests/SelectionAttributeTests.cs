using System.Linq;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Types
{
    public class SelectionAttributeTests
    {
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
                        }))
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



        private class Query
        {
            [UseSelection]
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
