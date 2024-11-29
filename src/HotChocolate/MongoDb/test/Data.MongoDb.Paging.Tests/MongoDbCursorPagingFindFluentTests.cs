using HotChocolate.Data.MongoDb.Filters;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Paging;

public class MongoDbCursorPagingFindFluentTests : IClassFixture<MongoResource>
{
    private readonly List<Foo> foos =
    [
        new Foo { Bar = "a", },
        new Foo { Bar = "b", },
        new Foo { Bar = "d", },
        new Foo { Bar = "e", },
        new Foo { Bar = "f", },
    ];

    private readonly MongoResource _resource;

    public MongoDbCursorPagingFindFluentTests(MongoResource resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task Simple_StringList_Default_Items()
    {
        // arrange
        var executor = await CreateSchemaAsync(requiresPagingBoundaries: false);

        // act
        var result = await executor.ExecuteAsync(
            @"{
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
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_First_2()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
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
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_First_2_After()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
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
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_Last_1_Before()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foos(last: 1 before: ""NA=="") {
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
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task Simple_StringList_Global_DefaultItem_2()
    {
        // arrange
        var executor = await CreateSchemaAsync(requiresPagingBoundaries: false);

        // act
        var result = await executor.ExecuteAsync(
            @"{
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
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task JustTotalCount()
    {
        // arrange
        var executor = await CreateSchemaAsync(requiresPagingBoundaries: false);

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foos {
                    totalCount
                }
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    [Fact]
    public async Task TotalCount_AndFirst()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foos(first:1) {
                    nodes {
                        bar
                    }
                    totalCount
                }
            }");

        // assert
        await Snapshot
            .Create()
            .AddResult(result)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Bar { get; set; } = default!;
    }

    private Func<IResolverContext, MongoDbCollectionExecutable<TResult>> BuildResolver<TResult>(
        MongoResource mongoResource,
        IEnumerable<TResult> results)
        where TResult : class
    {
        var collection =
            mongoResource.CreateCollection<TResult>("data_" + Guid.NewGuid().ToString("N"));

        collection.InsertMany(results);

        return ctx => collection.AsExecutable();
    }

    private ValueTask<IRequestExecutor> CreateSchemaAsync(bool requiresPagingBoundaries = true)
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddMongoDbPagingProviders()
            .AddFiltering(x => x.AddMongoDbDefaults())
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Field("foos")
                        .Resolve(BuildResolver(_resource, foos))
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
                        .UsePaging<ObjectType<Foo>>(
                            options: new PagingOptions { IncludeTotalCount = true, });
                })
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.ContextData.TryGetValue("query", out var queryString))
                    {
                        context.Result =
                            OperationResultBuilder
                                .FromResult(context.Result!.ExpectOperationResult())
                                .SetContextData("query", queryString)
                                .Build();
                    }
                })
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .ModifyPagingOptions(o => o.RequirePagingBoundaries = requiresPagingBoundaries)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
