using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JBooleanFilterTests : IClassFixture<Neo4jResource>
        //: SchemaCache
        //, IClassFixture<Neo4jResource>
    {
        private Neo4jResource _neo4JResource { get; }
        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        private static readonly Foo[] _fooEntities =
        {
            new() { Bar = true },
            new() { Bar = false }
        };

        public Neo4JBooleanFilterTests(Neo4jResource neo4jResource)
        {
            //Init(resource);
            _neo4JResource = neo4jResource;
        }

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = await CreateSchema<Foo, FooFilterType>(_fooEntities, _neo4JResource, false);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                    .Create());

            res1.MatchDocumentSnapshot("true");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                    .Create());

            res2.MatchDocumentSnapshot("false");
        }

        protected async Task<IRequestExecutor> CreateSchema<TEntity, T>(
            TEntity[] entities,
            Neo4jResource  neo4JResource,
            bool withPaging = false)
            where TEntity : class
            where T : FilterInputType<TEntity>
        {
            IAsyncSession session = _neo4JResource.GetAsyncSession();
            IResultCursor cursor = await session.RunAsync(@"CREATE (:Foo {bar: true}), (:Foo {bar: false})");
            await cursor.ConsumeAsync();

            return new ServiceCollection()
                .AddGraphQL()
                .AddFiltering(x => x.BindRuntimeType<TEntity, T>().AddNeo4JDefaults())
                .AddQueryType(
                    c => c
                        .Name("Query")
                        .Field("root")
                        .Resolver(new Neo4JExecutable<TEntity>(neo4JResource.GetAsyncSession()))
                        .Use(
                            next => async context =>
                            {
                                await next(context);
                                if (context.Result is IExecutable executable)
                                {
                                    context.ContextData["query"] = executable.Print();
                                }
                            })
                        .UseFiltering<T>())
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out var queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("query", queryString)
                                    .Create();
                        }
                    })
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .GetAwaiter()
                .GetResult();
        }

    }
}
