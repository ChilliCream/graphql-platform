using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.MongoDb.Data.Filters;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.MongoDb.Data.Paging
{
    public class MongoDbCursorPagingAggregateFluentTests : IClassFixture<MongoResource>
    {
        private readonly List<Foo> foos = new List<Foo>
        {
            new Foo { Bar = "a" },
            new Foo { Bar = "b" },
            new Foo { Bar = "d" },
            new Foo { Bar = "e" },
            new Foo { Bar = "f" }
        };

        private readonly MongoResource _resource;

        public MongoDbCursorPagingAggregateFluentTests(MongoResource resource)
        {
            _resource = resource;
        }

        [Fact]
        public async Task Simple_StringList_Default_Items()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            await executor
                .ExecuteAsync(
                    @"
                {
                    foos {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchDocumentSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_First_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            await executor
                .ExecuteAsync(
                    @"
                {
                    foos(first: 2) {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchDocumentSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_First_2_After()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();

            await executor
                .ExecuteAsync(
                    @"
                {
                    foos(first: 2 after: ""MQ=="") {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchDocumentSnapshotAsync();
        }

        [Fact]
        public async Task Simple_StringList_Global_DefaultItem_2()
        {
            Snapshot.FullName();

            IRequestExecutor executor = await CreateSchemaAsync();


            await executor
                .ExecuteAsync(
                    @"
                {
                    foos {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
                .MatchDocumentSnapshotAsync();
        }

        public class Foo
        {
            public string Bar { get; set; } = default!;
        }

        private Func<IResolverContext, IExecutable<TResult>> BuildResolver<TResult>(
            MongoResource mongoResource,
            IEnumerable<TResult> results)
            where TResult : class
        {
            IMongoCollection<TResult> collection =
                mongoResource.CreateCollection<TResult>("data_" + Guid.NewGuid().ToString("N"));

            collection.InsertMany(results);

            return ctx => collection.Aggregate().AsExecutable();
        }

        private ValueTask<IRequestExecutor> CreateSchemaAsync()
        {
            return new ServiceCollection()
                .AddGraphQL()
                .AddFiltering(x => x.AddMongoDbDefaults())
                .AddQueryType(
                    descriptor =>
                    {
                        descriptor
                            .Field("foos")
                            .Resolver(BuildResolver(_resource, foos))
                            .Type<ListType<ObjectType<Foo>>>()
                            .Use(
                                next => async context =>
                                {
                                    await next(context);
                                    if (context.Result is IExecutable executable)
                                    {
                                        context.ContextData["query"] = executable.Print();
                                    }
                                })
                            .UseMongoPaging<ObjectType<Foo>>();
                    })
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out object? queryString))
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
                .GetRequestExecutorAsync();
        }
    }
}
