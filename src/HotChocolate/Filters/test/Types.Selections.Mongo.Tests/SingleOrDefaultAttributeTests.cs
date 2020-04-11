using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultAttributeTests :
        IClassFixture<MongoResource>
    {
        private readonly MongoProvider _provider;

        public SingleOrDefaultAttributeTests(MongoResource provider)
        {
            _provider = new MongoProvider(provider);
        }

        [Fact]
        public virtual void Execute_Selection_MultipleScalarShouldReturnOne()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                Foo.Create("aa", 1, true));

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        }))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                 "{ foos { bar baz } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(1, resultCtx.Baz);
            Assert.Null(resultCtx.Nested);
            Assert.Null(resultCtx.NestedCollection);
        }

        [Fact]
        public virtual void Execute_Selection_ShouldThrowOnMultiple()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                Foo.Create("aa", 1, true),
                Foo.Create("bb", 2, true));

            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute("{ foos { bar baz} }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public virtual void Execute_SelectionMultiple_ShouldThrowOnMultiple()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                Foo.Create("aa", 1, true),
                Foo.Create("bb", 2, true));

            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.FoosMultiple)
                        .Resolver(resolver))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foosMultiple { bar baz } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public virtual void ExecuteAsync_Selection_MultipleScalarShouldReturnOne()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IAsyncEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateAsyncResolver(
                Foo.Create("aa", 1, true));

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.FoosAsync)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        }))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                 "{ foosAsync { bar baz } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Equal("aa", resultCtx.Bar);
            Assert.Equal(1, resultCtx.Baz);
        }

        [Fact]
        public virtual void ExecuteAsync_Selection_ShouldThrowOnMultiple()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IAsyncEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateAsyncResolver(
                Foo.Create("aa", 1, true),
                Foo.Create("bb", 2, true));

            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(
                    d => d.Field(t => t.FoosAsync)
                        .Resolver(resolver)
                        .UseSingleOrDefault()
                        .UseSelection())
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute("{ foosAsync { bar baz} }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public virtual void ExecuteAsync_SelectionMultiple_ShouldThrowOnMultiple()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IAsyncEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateAsyncResolver(
                Foo.Create("aa", 1, true),
                Foo.Create("bb", 2, true));

            Foo resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.FoosMultipleAsync)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as Foo;
                        }))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                "{ foosMultipleAsync { bar baz } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            [UseSingleOrDefault]
            [UseSelection]
            public IQueryable<Foo> Foos { get; }

            [UseFirstOrDefault]
            [UseSelection]
            public IQueryable<Foo> FoosMultiple { get; }

            [UseSingleOrDefault]
            [GraphQLType(typeof(ObjectType<Foo>))]
            public IAsyncEnumerator<Foo> FoosAsync { get; }

            [UseFirstOrDefault]
            [GraphQLType(typeof(ObjectType<Foo>))]
            public IAsyncEnumerator<Foo> FoosMultipleAsync { get; }
        }

        public class Foo
        {
            private static int idCounter = 1;

            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }

            public NestedFoo Nested { get; set; }

            public List<NestedFoo> NestedCollection { get; set; }

            public static Foo Create(string bar, int baz, bool setId)
            {
                var value = new Foo
                {
                    Bar = bar,
                    Baz = baz,
                    Nested = new NestedFoo()
                    {
                        Bar = "nested" + bar,
                        Baz = baz * 2
                    },
                    NestedCollection = new List<NestedFoo>()
                       {
                        new NestedFoo()
                        {
                            Bar = "nestedcollection" + bar,
                            Baz = baz * 3
                        },
                       }
                };
                if (setId)
                {
                    value.Id = ++idCounter;
                }
                return value;
            }
        }

        public class NestedFoo
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }
        }
    }
}
