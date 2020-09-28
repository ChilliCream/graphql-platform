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
    public class SingleOrDefaultAttributeTests :
        IClassFixture<SqlServerProvider>
    {
        private readonly SqlServerProvider _provider;

        public SingleOrDefaultAttributeTests(SqlServerProvider provider)
        {
            _provider = provider;
        }

        [Fact]
        public virtual void Execute_Selection_MultipleScalarShouldReturnOne()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                Foo.Create("aa", 1, false));

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
            IRequestExecutor executor = schema.MakeExecutable();

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
                Foo.Create("aa", 1, false),
                Foo.Create("bb", 2, false));

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
        public virtual void Execute_SelectionMultiple_ShouldThrowOnMultiple()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(
                Foo.Create("aa", 1, false),
                Foo.Create("bb", 2, false));

            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.FoosMultiple)
                        .Resolver(resolver))
                .Create();
            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = executor.Execute(
                 "{ foosMultiple { bar baz } }");

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
