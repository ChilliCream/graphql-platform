using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionTests :
        IClassFixture<MongoResource>
    {
        private readonly static Foo[] SAMPLE =
            new[] {
                Foo.Create("aa", 1),
                Foo.Create("bb", 2) };

        private readonly MongoProvider _provider;

        public SelectionTests(MongoResource provider)
        {
            _provider = new MongoProvider(provider);
        }

        [Fact]
        public virtual void Execute_Selection_Fragment()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

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
                    Assert.Null(x.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(2, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Scalar()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

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
                    Assert.Null(x.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_MultipleScalar()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

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
                    Assert.Null(x.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(2, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_ScalarList()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foos { stringList } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.ObjectArray);
                    Assert.NotNull(x.StringList);
                    Assert.Equal(2, x.StringList.Count);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.ObjectArray);
                    Assert.NotNull(x.StringList);
                    Assert.Equal(2, x.StringList.Count);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Object()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

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
                    Assert.Equal(0, x.Nested.Baz);
                    Assert.Null(x.ObjectArray);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.NotNull(x.Nested);
                    Assert.Equal("nestedbb", x.Nested.Bar);
                    Assert.Equal(0, x.Nested.Baz);
                    Assert.Null(x.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_ObjectDeep()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { nested { nested { nested { nested { bar } } } } } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.NotNull(x.Nested);
                    Assert.Equal(0, x.Nested.Baz);
                    Assert.Null(x.ObjectArray);
                    Assert.Null(x.Nested.ObjectArray);
                    Assert.Null(x.Nested.Nested.Bar);
                    Assert.Null(x.Nested.Nested.Nested.Bar);
                    Assert.Equal("recursiveaa", x.Nested.Nested.Nested.Nested.Bar);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.NotNull(x.Nested);
                    Assert.Equal(0, x.Nested.Baz);
                    Assert.Null(x.ObjectArray);
                    Assert.Null(x.Nested.ObjectArray);
                    Assert.Null(x.Nested.Nested.Bar);
                    Assert.Null(x.Nested.Nested.Nested.Bar);
                    Assert.Equal("recursivebb", x.Nested.Nested.Nested.Nested.Bar);
                });
        }

        [Fact]
        public virtual void Execute_Selection_ObjectDeepNull()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        })
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                "{ foos { nestedNull { nestedNull { nestedNull { nestedNull { bar } } } } } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                },
                x =>
                {
                    Assert.Null(x.Bar);
                    Assert.Equal(0, x.Baz);
                    Assert.Null(x.Nested);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Root_Sorting()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseFiltering()
                        .UseSorting()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foos(order_by: {bar: DESC}) { nodes { bar } } }");

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.Equal("bb", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("aa", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Root_Filtering_OfNotSelectedField()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseSelection()
                        .UseFiltering()
                        .UseSorting())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foos(where: {bar: \"aa\"}) { nodes { id } } }");

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.Null(x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Root_Sorting_OfNotSelectedField()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseSelection()
                        .UseFiltering()
                        .UseSorting())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foos(order_by: {id: DESC}) { nodes { bar } } }");

            // assert
            Assert.Null(result.Errors);
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.NotNull(x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                },
                x =>
                {
                    Assert.NotNull(x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Object_Paging_Nodes()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseFiltering()
                        .UseSorting()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { nodes { nested { bar } } } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.Null(x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.NotNull(x.Node.Nested);
                    Assert.Equal("nestedaa", x.Node.Nested.Bar);
                    Assert.Equal(0, x.Node.Nested.Baz);
                    Assert.Null(x.Node.ObjectArray);
                },
                x =>
                {
                    Assert.Null(x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.NotNull(x.Node.Nested);
                    Assert.Equal("nestedbb", x.Node.Nested.Bar);
                    Assert.Equal(0, x.Node.Nested.Baz);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Object_Paging_Edges()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseFiltering()
                        .UseSorting()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { edges { node { bar }} } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("bb", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.Null(x.Node.Nested);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        [Fact]
        public virtual void Execute_Selection_Object_Paging_Combined()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            Connection<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Connection<Foo>;
                        })
                        .UsePaging<ObjectType<Foo>>()
                        .UseFiltering()
                        .UseSorting()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foos { nodes { nested { bar } } edges { node { bar }} } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.Edges.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.NotNull(x.Node.Nested);
                    Assert.Equal("nestedaa", x.Node.Nested.Bar);
                    Assert.Equal(0, x.Node.Nested.Baz);
                    Assert.Null(x.Node.ObjectArray);
                },
                x =>
                {
                    Assert.Equal("bb", x.Node.Bar);
                    Assert.Equal(0, x.Node.Baz);
                    Assert.NotNull(x.Node.Nested);
                    Assert.Equal("nestedbb", x.Node.Nested.Bar);
                    Assert.Equal(0, x.Node.Nested.Baz);
                    Assert.Null(x.Node.ObjectArray);
                });
        }

        public class Query
        {
            public IQueryable<Foo> Foos { get; }
        }

        public class Foo
        {
            [Key]
            public Guid Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }

            public List<string> StringList { get; set; }

            public NestedFoo Nested { get; set; }

            public NestedFoo NestedNull { get; set; }

            public NestedFoo[] ObjectArray { get; set; }

            public List<NestedFoo> ObjectList { get; set; }

            public IList<NestedFoo> IObjectList { get; set; }

            public HashSet<NestedFoo> HashSet { get; set; }

            public SortedSet<NestedFoo> SortedSet { get; set; }

            public ISet<NestedFoo> ISet { get; set; }

            [UsePaging]
            [UseFiltering]
            [UseSorting]
            public List<NestedFoo> MiddlewareList { get; set; }

            public string GetComputedField() => Bar + Baz;

            public string GetComputedFieldParent([Parent]Foo foo) => foo.Bar + foo.Baz;

            public static Foo Create(string bar, int baz)
            {
                var recursive = new NestedFoo()
                {
                    Bar = "recursive" + bar,
                    Baz = 10
                };
                var recursive2 = new NestedFoo()
                {
                    Bar = "recursive" + bar,
                    Baz = 10
                };
                for (var level = 0; level < 10; level++)
                {
                    recursive = new NestedFoo()
                    {
                        Bar = "recursive" + bar,
                        Baz = 10,
                        Nested = recursive.Clone(),
                        ObjectArray = new List<NestedFoo> { recursive2.Clone() }
                    };

                    recursive2 = new NestedFoo()
                    {
                        Bar = "recursive" + bar,
                        Baz = 10,
                        Nested = recursive.Clone(),
                        ObjectArray = new List<NestedFoo> { recursive2.Clone() }
                    };
                }
                return new Foo
                {
                    Bar = bar,
                    Baz = baz,
                    StringList =
                        new List<string> { "stringListMember0" + bar, "stringListMember1" + bar },
                    Nested = new NestedFoo()
                    {
                        Bar = "nested" + bar,
                        Baz = baz * 2,
                        Nested = recursive.Clone(),
                        ObjectArray = new List<NestedFoo> { recursive2.Clone() }
                    },
                    ObjectArray = new NestedFoo[]
                       {
                        new NestedFoo()
                        {
                            Bar = "objectArray" + bar,
                            Baz = baz * 3,
                            Nested = recursive.Clone(),
                            ObjectArray = new List<NestedFoo> { recursive2.Clone() }
                        },
                       },
                    ObjectList = new List<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "objectList" + bar,
                            Baz = baz * 3
                        },
                       },
                    IObjectList = new List<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "iList" + bar,
                            Baz = baz * 3
                        },
                       },
                    HashSet = new HashSet<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "hashSet" + bar,
                            Baz = baz * 3
                        },
                       },
                    SortedSet = new SortedSet<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "sortedSet" + bar,
                            Baz = baz * 3
                        },
                       },
                    ISet = new HashSet<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "iSet" + bar,
                            Baz = baz * 3
                        },
                       },
                    MiddlewareList = new List<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "cc" + bar,
                            Baz = baz * 1
                        },
                        new NestedFoo()
                        {
                            Bar = "aa" + bar,
                            Baz = baz * 1
                        },
                        new NestedFoo()
                        {
                            Bar = "bb" + bar,
                            Baz = baz * 2
                        },
                    }
                };
            }
        }

        public class NestedFoo
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }

            public NestedFoo NestedNull { get; set; }

            public NestedFoo Nested { get; set; }

            public List<NestedFoo> ObjectArray { get; set; }

            public NestedFoo Clone()
            {
                NestedFoo clone = (NestedFoo)base.MemberwiseClone();
                return clone;
            }
        }
    }
}
