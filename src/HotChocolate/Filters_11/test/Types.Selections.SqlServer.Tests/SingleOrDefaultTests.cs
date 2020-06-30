using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultTests :
        IClassFixture<SqlServerProvider>
    {
        private readonly static Foo[] Sample =
            new[] { Foo.Create("aa", 1) };

        private readonly SqlServerProvider _provider;

        public SingleOrDefaultTests(SqlServerProvider provider)
        {
            _provider = provider;
        }

        [Fact]
        public virtual void Execute_Single_ShouldThrowWhenMoreThanOne()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                new[] { Foo.Create("aa", 1), Foo.Create("aa", 2) });

            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute("{ foos { bar baz} }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public virtual void Execute_Single_PureCodeFirst()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(Sample);

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        })
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute("{ foos { bar  } }");

            // assert
            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(0, resultCtx.Baz);
        }

        [Fact]
        public virtual void Execute_Single_OverrideListType()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(Sample);

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        })
                        .Type<ListType<FooType>>()
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult executionResult = executor.Execute("{ foos { fakeBar } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(0, resultCtx.Baz);
            Snapshot.Match(executionResult, "execute_single_overrides_results");
            Snapshot.Match(schema.ToString(), "execute_single_overrides_schema");
        }

        [Fact]
        public virtual void Execute_Single_OverrideNonNullType()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(Sample);

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        })
                        .Type<NonNullType<FooType>>()
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult executionResult = executor.Execute("{ foos { fakeBar } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(0, resultCtx.Baz);
            Snapshot.Match(executionResult, "execute_single_overrides_results");
            Snapshot.Match(schema.ToString(), "execute_single_overrides_schema_nonnullable");
        }

        [Fact]
        public virtual void Execute_Single_OverrideNonNullListType()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(Sample);

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        })
                        .Type<NonNullType<ListType<NonNullType<FooType>>>>()
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult executionResult = executor.Execute("{ foos { fakeBar } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(0, resultCtx.Baz);
            Snapshot.Match(executionResult, "execute_single_overrides_results");
            Snapshot.Match(schema.ToString(), "execute_single_overrides_schema_nonnullable");
        }

        [Fact]
        public virtual void Execute_Single_OverrideTypeOnField()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(Sample);

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        })
                        .Type<FooType>()
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult executionResult = executor.Execute("{ foos { fakeBar } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(0, resultCtx.Baz);
            Snapshot.Match(executionResult, "execute_single_overrides_results");
            Snapshot.Match(schema.ToString(), "execute_single_overrides_schema");
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

            [UseSingleOrDefault]
            [UseFiltering]
            [UseSorting]
            public List<NestedFoo> MiddlewareList { get; set; }


            public static Foo Create(string bar, int baz)
            {
                return new Foo
                {
                    Bar = bar,
                    Baz = baz,
                    MiddlewareList = new List<NestedFoo>
                       {
                        new NestedFoo()
                        {
                            Bar = "aa" + bar,
                            Baz = baz * 1
                        },
                        new NestedFoo()
                        {
                            Bar = "cc" + bar,
                            Baz = baz * 1
                        }
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

            public NestedFoo Clone()
            {
                NestedFoo clone = (NestedFoo)base.MemberwiseClone();
                return clone;
            }
        }

        public class FooType : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                base.Configure(descriptor);
                descriptor.Field(x => x.Bar).Name("fakeBar");
            }
        }
    }
}
