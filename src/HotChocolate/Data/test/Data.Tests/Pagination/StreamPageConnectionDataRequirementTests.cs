using System.Collections;
using System.Data;
using System.Data.Common;
using CookieCrumble.Resources;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPageConnectionDataRequirementTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Execute_Should_ReadOnlyTheRequiredRowsBeforeTheResolverReturns_When_OnlyNodesAreSelected()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } } }",
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        await result.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_EdgesAndNodesAreSelected()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { edges { cursor node { id } } nodes { id } } }",
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        await result.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "edges": [
                    {
                      "cursor": "e30x",
                      "node": {
                        "id": 1
                      }
                    },
                    {
                      "cursor": "e30y",
                      "node": {
                        "id": 2
                      }
                    }
                  ],
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_NodesHasNextPageAndEndCursorAreSelected()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } pageInfo { hasNextPage endCursor } } }",
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        await result.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "pageInfo": {
                    "hasNextPage": true,
                    "endCursor": "e30y"
                  }
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_OnlyTotalCountIsSelected()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { totalCount } }",
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        await result.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "totalCount": 5
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_RelativeCursorsAreSelected()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 9);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            """
            {
              items(first: 2) {
                nodes { id }
                pageInfo { forwardCursors { page } backwardCursors { page } }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        await result.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "pageInfo": {
                    "forwardCursors": [
                      {
                        "page": 2
                      },
                      {
                        "page": 3
                      },
                      {
                        "page": 4
                      },
                      {
                        "page": 5
                      }
                    ],
                    "backwardCursors": []
                  }
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_LeaveDeferredMembersToTheirOwnPayload_When_NodesAreDeferred()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { totalCount ... @defer { nodes { id } } } }",
            TestContext.Current.CancellationToken);

        await foreach (var payload in result.ExpectResponseStream().ReadResultsAsync())
        {
            await payload.DisposeAsync();
        }

        await result.DisposeAsync();

        // assert
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": true
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotLoadStreamedMembers_When_NodesAreStreamed()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes @stream(initialCount: 1) { id } } }",
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": true
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_LoadStreamedMembers_When_StreamIsDisabledByLiteral()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes @stream(if: false, initialCount: 1) { id } } }",
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Execute_Should_HonorTheStreamCondition_When_TheConditionIsAVariable(
        bool stream,
        bool expectedReadAfterResolver)
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($s: Boolean!) {
                      items(first: 2) {
                        nodes @stream(if: $s, initialCount: 1) { id }
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["s"] = stream })
                .Build(),
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        Assert.Equal(expectedReadAfterResolver, store.Summarize().ReadAfterResolver);
    }

    [Fact]
    public async Task Execute_Should_LeaveStreamedMembersToTheStream_When_StreamConditionIsTrueLiteral()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes @stream(if: true, initialCount: 1) { id } } }",
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": true
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_LeaveStreamedMembersToTheStream_When_NullableConditionVariableIsNotProvided()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            """
            query($s: Boolean) {
              items(first: 2) {
                nodes @stream(if: $s, initialCount: 1) { id }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": true
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ReportAnError_When_NullableConditionVariableIsNull()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($s: Boolean) {
                      items(first: 2) {
                        nodes @stream(if: $s, initialCount: 1) { id }
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["s"] = null })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The variable `s` is null, but the `if` argument of the `@stream` directive must not be null.",
                  "path": [
                    "items"
                  ],
                  "extensions": {
                    "code": "HC0018",
                    "variable": "s",
                    "directive": "stream"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_RejectTheOperation_When_StreamConditionIsNullLiteral()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes @stream(if: null, initialCount: 1) { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The specified argument value does not match the argument type.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 39
                    }
                  ],
                  "path": [
                    "items",
                    "nodes"
                  ],
                  "extensions": {
                    "argument": "if",
                    "argumentValue": "null",
                    "locationType": "Boolean!",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Values-of-Correct-Type"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_TheSameDocumentRunsWithOtherIncludeVariables()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);
        const string document =
            """
            query($nodes: Boolean!, $count: Boolean!) {
              items(first: 2) {
                nodes @include(if: $nodes) { id }
                totalCount @include(if: $count)
              }
            }
            """;

        // act
        await using var countOnly = await executor.ExecuteAsync(
            CreateRequest(document, ("nodes", false), ("count", true)),
            TestContext.Current.CancellationToken);
        await using var nodesOnly = await executor.ExecuteAsync(
            CreateRequest(document, ("nodes", true), ("count", false)),
            TestContext.Current.CancellationToken);
        var json = nodesOnly.ToJson();
        await nodesOnly.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_NotReadAfterTheResolverReturns_When_TheSameDocumentRunsWithOtherStreamVariables()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);
        const string document =
            """
            query($stream: Boolean!) {
              items(first: 2) {
                nodes @stream(if: $stream, initialCount: 1) { id }
              }
            }
            """;

        // act
        await using var streamed = await executor.ExecuteAsync(
            CreateRequest(document, ("stream", true)),
            TestContext.Current.CancellationToken);
        await streamed.DisposeAsync();
        await using var loaded = await executor.ExecuteAsync(
            CreateRequest(document, ("stream", false)),
            TestContext.Current.CancellationToken);
        var json = loaded.ToJson();
        await loaded.DisposeAsync();

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Theory]
    [InlineData("items(first: 2) { nodes { id } }")]
    [InlineData("items(first: 2) { edges { cursor node { id } } nodes { id } }")]
    [InlineData("items(first: 2) { nodes { id } pageInfo { hasNextPage endCursor } }")]
    [InlineData("items(first: 2) { nodes { id } pageInfo { hasPreviousPage startCursor } totalCount }")]
    [InlineData("items(first: 2) { nodes { id } pageInfo { forwardCursors { page } backwardCursors { page } } }")]
    [InlineData("items(first: 2) { totalCount }")]
    [InlineData("items(last: 2) { pageInfo { startCursor } }")]
    [InlineData("items(last: 2) { pageInfo { hasPreviousPage startCursor } }")]
    [InlineData("items(last: 2, before: \"e30y\") { pageInfo { startCursor } }")]
    public async Task Execute_Should_NotReadAfterTheResolversReturn_When_PagesAreBatched(string items)
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 2, itemsPerAuthor: 9);
        var executor = await CreateExecutorAsync(store);

        // act
        await using var result = await executor.ExecuteAsync(
            $"{{ authors {{ id {items} }} }}",
            TestContext.Current.CancellationToken);
        var errorCount = result.ExpectOperationResult().Errors.Count;
        await result.DisposeAsync();

        // assert
        Assert.Equal(0, errorCount);
        Assert.Equal(2, store.ResolverReturns);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_LoadRequirementsAndDisposeAtRequestEnd_When_TheFieldIsSourceGenerated()
    {
        // arrange
        var store = await Store.CreateAsync(resource, authorCount: 1, itemsPerAuthor: 5);
        var executor = await new ServiceCollection()
            .AddSingleton(store)
            .AddGraphQLServer()
            .AddType<global::HotChocolate.Types.Pagination.ItemConnectionType>()
            .AddType<global::HotChocolate.Types.Pagination.ItemEdgeType>()
            .ConfigureDescriptorContext(
                context => context.TypeConfiguration.TryAdd(
                    "StreamPageConnectionTests::HotChocolate.Data.Pagination.GeneratedQuery",
                    OperationTypeNames.Query,
                    () => GeneratedQuery.Initialize))
            .ConfigureSchema(
                builder => builder.TryAddRootType(
                    () => new ObjectType(descriptor => descriptor.Name(OperationTypeNames.Query)),
                    OperationType.Query))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ generatedItems(first: 2) { nodes { id } pageInfo { hasNextPage endCursor } totalCount } }",
            TestContext.Current.CancellationToken);
        var json = result.ToJson();
        var releasedBefore = store.Released;
        await result.DisposeAsync();
        var releasedAfter = store.Released;

        // assert
        json.MatchInlineSnapshot(
            """
            {
              "data": {
                "generatedItems": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "pageInfo": {
                    "hasNextPage": true,
                    "endCursor": "e30y"
                  },
                  "totalCount": 5
                }
              }
            }
            """);
        store.Summarize().MatchInlineSnapshot(
            """
            {
              "RowsWereRead": true,
              "ReadAfterResolver": false
            }
            """);
        Assert.Equal(1, releasedBefore);
        Assert.Equal(1, releasedAfter);
    }

    private static IOperationRequest CreateRequest(string document, params (string Name, bool Value)[] variables)
        => OperationRequestBuilder.New()
            .SetDocument(document)
            .SetVariableValues(variables.ToDictionary(t => t.Name, t => (object?)t.Value))
            .Build();

    private static async Task<IRequestExecutor> CreateExecutorAsync(Store store)
        => await new ServiceCollection()
            .AddSingleton(store)
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddType<AuthorType>()
            .AddType<ItemConnectionType>()
            .AddDataLoader<ItemsByAuthorDataLoader>()
            .AddPagingArguments()
            .ModifyOptions(options =>
            {
                options.EnableDefer = true;
                options.EnableStream = true;
            })
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = 20_000;
                options.MaxTypeCost = 20_000;
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public sealed class Item
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }
    }

    public sealed class Author
    {
        public int Id { get; set; }

        [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] ItemsByAuthorDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            var page = await dataLoader
                .With(arguments with { EnableRelativeCursors = true })
                .LoadAsync(Id, cancellationToken);

            return page ?? StreamPage<Item>.Empty;
        }
    }

    public sealed class Query
    {
        [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] Store store,
            CancellationToken cancellationToken)
        {
            var context = store.CreateContext();

            return await context.Items
                .OrderBy(item => item.Id)
                .ToStreamPageAsync(
                    arguments with { EnableRelativeCursors = true },
                    store.CreateLifetime(context),
                    cancellationToken);
        }

        public List<Author> GetAuthors([Service] Store store)
            => store.AuthorIds.Select(id => new Author { Id = id }).ToList();
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
            => descriptor
                .Field(query => query.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>()
                .Use(RecordResolverReturn);

        private static FieldDelegate RecordResolverReturn(FieldDelegate next)
            => async context =>
            {
                await next(context);
                context.Service<Store>().RecordResolverReturn();
            };
    }

    public sealed class AuthorType : ObjectType<Author>
    {
        protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
            => descriptor
                .Field(author => author.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>()
                .Use(RecordResolverReturn);

        private static FieldDelegate RecordResolverReturn(FieldDelegate next)
            => async context =>
            {
                await next(context);
                context.Service<Store>().RecordResolverReturn();
            };
    }

    public sealed class ItemConnectionType : ObjectType<StreamPageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageConnection<Item>> descriptor)
            => descriptor.Name("ItemConnection");
    }

    public sealed class ItemsByAuthorDataLoader(
        Store store,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, StreamPage<Item>>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, StreamPage<Item>>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<StreamPage<Item>> context,
            CancellationToken cancellationToken)
        {
            var arguments = context.GetPagingArguments();
            var dbContext = store.CreateContext();

            return await dbContext.Items
                .Where(item => keys.Contains(item.AuthorId))
                .OrderBy(item => item.Id)
                .ToBatchStreamPageAsync(
                    item => item.AuthorId,
                    arguments,
                    store.CreateLifetime(dbContext),
                    cancellationToken);
        }
    }

    public sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    public sealed class Store : DbCommandInterceptor
    {
        private readonly DbContextOptions<PagingContext> _options;
        private readonly object _sync = new();
        private volatile bool _counting;
        private int _reads;
        private int _readsAfterResolver;
        private int _resolverReturns;
        private int _released;

        private Store(string connectionString, int[] authorIds)
        {
            AuthorIds = authorIds;
            _options = new DbContextOptionsBuilder<PagingContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(this)
                .Options;
        }

        public int[] AuthorIds { get; }

        public int ReadsAtEnd => Volatile.Read(ref _reads);

        public int ReadsAfterResolver
        {
            get
            {
                lock (_sync)
                {
                    return _readsAfterResolver;
                }
            }
        }

        public int Released => Volatile.Read(ref _released);

        public int ResolverReturns
        {
            get
            {
                lock (_sync)
                {
                    return _resolverReturns;
                }
            }
        }

        public static async Task<Store> CreateAsync(
            PostgreSqlResource resource,
            int authorCount,
            int itemsPerAuthor)
        {
            var authorIds = Enumerable.Range(1, authorCount).ToArray();
            var store = new Store(resource.GetConnectionString($"db_{Guid.NewGuid():N}"), authorIds);

            await using var context = store.CreateContext();
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            foreach (var authorId in authorIds)
            {
                for (var i = 0; i < itemsPerAuthor; i++)
                {
                    context.Items.Add(new Item { AuthorId = authorId });
                }
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            store._counting = true;

            return store;
        }

        public PagingContext CreateContext() => new(_options);

        public IAsyncDisposable CreateLifetime(PagingContext context) => new Lifetime(this, context);

        public ReadSummary Summarize()
            => new(ReadsAtEnd > 0, ReadsAtEnd > ReadsAfterResolver);

        public void RecordResolverReturn()
        {
            lock (_sync)
            {
                _resolverReturns++;
                _readsAfterResolver = Math.Max(_readsAfterResolver, Volatile.Read(ref _reads));
            }
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
            => _counting
                ? new(new CountingDataReader(result, () => Interlocked.Increment(ref _reads)))
                : base.ReaderExecutedAsync(command, eventData, result, cancellationToken);

        private sealed class Lifetime(Store store, PagingContext context) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref store._released);
                await context.DisposeAsync();
            }
        }
    }

    public sealed record ReadSummary(bool RowsWereRead, bool ReadAfterResolver);

    private sealed class CountingDataReader(DbDataReader inner, Action onRead) : DbDataReader
    {
        public override object this[int ordinal] => inner[ordinal];

        public override object this[string name] => inner[name];

        public override int Depth => inner.Depth;

        public override int FieldCount => inner.FieldCount;

        public override bool HasRows => inner.HasRows;

        public override bool IsClosed => inner.IsClosed;

        public override int RecordsAffected => inner.RecordsAffected;

        public override int VisibleFieldCount => inner.VisibleFieldCount;

        public override bool Read()
        {
            onRead();
            return inner.Read();
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            onRead();
            return inner.ReadAsync(cancellationToken);
        }

        public override bool NextResult() => inner.NextResult();

        public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
            => inner.NextResultAsync(cancellationToken);

        public override bool GetBoolean(int ordinal) => inner.GetBoolean(ordinal);

        public override byte GetByte(int ordinal) => inner.GetByte(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
            => inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        public override char GetChar(int ordinal) => inner.GetChar(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
            => inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        public override string GetDataTypeName(int ordinal) => inner.GetDataTypeName(ordinal);

        public override DateTime GetDateTime(int ordinal) => inner.GetDateTime(ordinal);

        public override decimal GetDecimal(int ordinal) => inner.GetDecimal(ordinal);

        public override double GetDouble(int ordinal) => inner.GetDouble(ordinal);

        public override Type GetFieldType(int ordinal) => inner.GetFieldType(ordinal);

        public override T GetFieldValue<T>(int ordinal) => inner.GetFieldValue<T>(ordinal);

        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
            => inner.GetFieldValueAsync<T>(ordinal, cancellationToken);

        public override float GetFloat(int ordinal) => inner.GetFloat(ordinal);

        public override Guid GetGuid(int ordinal) => inner.GetGuid(ordinal);

        public override short GetInt16(int ordinal) => inner.GetInt16(ordinal);

        public override int GetInt32(int ordinal) => inner.GetInt32(ordinal);

        public override long GetInt64(int ordinal) => inner.GetInt64(ordinal);

        public override string GetName(int ordinal) => inner.GetName(ordinal);

        public override int GetOrdinal(string name) => inner.GetOrdinal(name);

        public override string GetString(int ordinal) => inner.GetString(ordinal);

        public override object GetValue(int ordinal) => inner.GetValue(ordinal);

        public override int GetValues(object[] values) => inner.GetValues(values);

        public override bool IsDBNull(int ordinal) => inner.IsDBNull(ordinal);

        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
            => inner.IsDBNullAsync(ordinal, cancellationToken);

        public override DataTable? GetSchemaTable() => inner.GetSchemaTable();

        public override IEnumerator GetEnumerator() => ((IEnumerable)inner).GetEnumerator();

        public override void Close() => inner.Close();

        public override Task CloseAsync() => inner.CloseAsync();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
        }

        public override ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}

[QueryType]
public static partial class GeneratedQuery
{
    [UseConnection(IncludeTotalCount = true)]
    public static async Task<StreamPageConnection<StreamPageConnectionDataRequirementTests.Item>> GetGeneratedItems(
        PagingArguments arguments,
        [Service] StreamPageConnectionDataRequirementTests.Store store,
        CancellationToken cancellationToken)
    {
        var context = store.CreateContext();

        return await context.Items
            .OrderBy(item => item.Id)
            .ToStreamPageAsync(
                arguments,
                store.CreateLifetime(context),
                cancellationToken);
    }

    static partial void Configure(IObjectTypeDescriptor descriptor)
        => descriptor
            .Field("generatedItems")
            .Extend()
            .OnBeforeCreate(
                (_, configuration) => configuration.MiddlewareConfigurations.Insert(
                    0,
                    new FieldMiddlewareConfiguration(RecordResolverReturn)));

    private static FieldDelegate RecordResolverReturn(FieldDelegate next)
        => async context =>
        {
            await next(context);
            context.Service<StreamPageConnectionDataRequirementTests.Store>().RecordResolverReturn();
        };
}
