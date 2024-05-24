using CookieCrumble;
using HotChocolate.Data.Raven.Filters;
using HotChocolate.Data.Raven.Paging;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class RavenAsyncDocumentQueryTests
{
    private readonly List<Foo> foos =
    [
        new Foo { Bar = "a", },
        new Foo { Bar = "b", },
        new Foo { Bar = "d", },
        new Foo { Bar = "e", },
        new Foo { Bar = "f", },
    ];

    private readonly SchemaCache _resource;

    public RavenAsyncDocumentQueryTests(SchemaCache resource)
    {
        _resource = resource;
    }

    [Fact]
    public async Task Cursor_ObjectList_Default_Items()
    {
        // arrange
        var executor = await CreateSchemaAsync();

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
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Cursor_ObjectList_First_2()
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
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Cursor_ObjectList_First_2_After()
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
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                        startCursor
                        endCursor
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Cursor_ObjectList_Global_DefaultItem_2()
    {
        // arrange
        var executor = await CreateSchemaAsync();

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
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Cursor_JustTotalCount()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foos {
                    totalCount
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Cursor_TotalCount_AndFirst()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foos(first:1) {
                    totalCount
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_ObjectList_Default_Items()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset {
                    items {
                        bar
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_ObjectList_Take_2()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset(take: 2) {
                    items {
                        bar
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_ObjectList_Take_2_After_2()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset(take: 2 skip: 2) {
                    items {
                        bar
                    }
                    totalCount
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_ObjectList_Global_DefaultItem_2()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset {
                    items {
                        bar
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_JustTotalCount()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset {
                    totalCount
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    [Fact]
    public async Task Offset_TotalCount_AndTake()
    {
        // arrange
        var executor = await CreateSchemaAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                foosOffset(take:1) {
                    totalCount
                }
            }");

        // assert
        await Snapshot.Create().AddResult(result).MatchAsync();
    }

    public class Foo
    {
        public string? Id { get; set; }

        public string Bar { get; set; } = default!;
    }

    private Func<IResolverContext, IAsyncDocumentQuery<TResult>> BuildResolver<TResult>(
        IDocumentStore store,
        IEnumerable<TResult> results)
        where TResult : class
    {
        using var session = store.OpenSession();

        foreach (var item in results)
        {
            session.Store(item);
        }

        session.SaveChanges();

        return ctx
            => ((IAsyncDocumentSession)ctx.LocalContextData["session"]!).Advanced
            .AsyncDocumentQuery<TResult>();
    }

    private ValueTask<IRequestExecutor> CreateSchemaAsync()
    {
        var database = _resource.CreateDatabase();

        return new ServiceCollection()
            .AddSingleton(database)
            .AddGraphQL()
            .AddRavenPagingProviders()
            .AddRavenFiltering()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Field("foos")
                        .Resolve(BuildResolver(database, foos))
                        .Type<ListType<ObjectType<Foo>>>()
                        .Use(
                            next => async context =>
                            {
                                using (var session = database.OpenAsyncSession())
                                {
                                    context.LocalContextData =
                                        context.LocalContextData.SetItem("session", session);
                                    await next(context);
                                }
                            })
                        .Use(
                            next => async context =>
                            {
                                await next(context);
                                if (context.Result is IRavenQueryable<Foo> queryable)
                                {
                                    context.ContextData["sql"] = queryable.ToString();
                                }
                            })
                        .UsePaging<ObjectType<Foo>>(
                            options: new PagingOptions { IncludeTotalCount = true, });

                    descriptor
                        .Field("foosOffset")
                        .Resolve(BuildResolver(database, foos))
                        .Type<ListType<ObjectType<Foo>>>()
                        .Use(
                            next => async context =>
                            {
                                using (var session = database.OpenAsyncSession())
                                {
                                    context.LocalContextData =
                                        context.LocalContextData.SetItem("session", session);
                                    await next(context);
                                }
                            })
                        .Use(
                            next => async context =>
                            {
                                await next(context);
                                if (context.Result is IRavenQueryable<Foo> queryable)
                                {
                                    context.ContextData["sql"] = queryable.ToString();
                                }
                            })
                        .UseOffsetPaging<ObjectType<Foo>>(
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
                                .FromResult(context.Result!.ExpectQueryResult())
                                .SetContextData("query", queryString)
                                .Build();
                    }
                })
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }
}
