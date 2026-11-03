using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using CookieCrumble.Resources;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPageConnectionExecutionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Execute_Should_ReturnEquivalentPagesPerAuthor_When_BatchStreamPageConnectionIsQueried()
    {
        // arrange
        var store = await Store.CreateAsync(
            resource.GetConnectionString($"db_{Guid.NewGuid():N}"),
            authorCount: 2,
            itemsPerAuthor: 4);
        var executor = await CreateExecutorAsync(store);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                      authors {
                        id
                        items(first: 2) {
                          nodes { id authorId }
                          edges { cursor node { id authorId } }
                          pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                          totalCount
                        }
                        pagedItems(first: 2) {
                          nodes { id authorId }
                          edges { cursor node { id authorId } }
                          pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                          totalCount
                        }
                      }
                    }
                    """)
                .Build(),
            Xunit.TestContext.Current.CancellationToken);
        var resultJson = result.ToJson();
        await result.DisposeAsync();

        using var document = JsonDocument.Parse(resultJson);
        var authors = document.RootElement.GetProperty("data").GetProperty("authors");
        var batchStreamPages = authors
            .EnumerateArray()
            .Select(author => JsonSerializer.Serialize(author.GetProperty("items")))
            .ToArray();
        var batchPagedPages = authors
            .EnumerateArray()
            .Select(author => JsonSerializer.Serialize(author.GetProperty("pagedItems")))
            .ToArray();
        var singlePagedPages = new List<string>();

        foreach (var author in authors.EnumerateArray())
        {
            var authorId = author.GetProperty("id").GetInt32();
            var singleResult = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        $$"""
                        {
                          pagedItems(authorId: {{authorId}}, first: 2) {
                            nodes { id authorId }
                            edges { cursor node { id authorId } }
                            pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                            totalCount
                          }
                        }
                        """)
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);
            var singleResultJson = singleResult.ToJson();
            await singleResult.DisposeAsync();

            using var singleDocument = JsonDocument.Parse(singleResultJson);
            singlePagedPages.Add(
                JsonSerializer.Serialize(
                    singleDocument.RootElement
                        .GetProperty("data")
                        .GetProperty("pagedItems")));
        }

        // assert
        Assert.Equal(batchPagedPages, batchStreamPages);
        Assert.Equal(singlePagedPages, batchStreamPages);
        resultJson.MatchInlineSnapshot(
            """
            {
              "data": {
                "authors": [
                  {
                    "id": 1,
                    "items": {
                      "nodes": [
                        {
                          "id": 1,
                          "authorId": 1
                        },
                        {
                          "id": 2,
                          "authorId": 1
                        }
                      ],
                      "edges": [
                        {
                          "cursor": "e30x",
                          "node": {
                            "id": 1,
                            "authorId": 1
                          }
                        },
                        {
                          "cursor": "e30y",
                          "node": {
                            "id": 2,
                            "authorId": 1
                          }
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true,
                        "hasPreviousPage": false,
                        "startCursor": "e30x",
                        "endCursor": "e30y"
                      },
                      "totalCount": 4
                    },
                    "pagedItems": {
                      "nodes": [
                        {
                          "id": 1,
                          "authorId": 1
                        },
                        {
                          "id": 2,
                          "authorId": 1
                        }
                      ],
                      "edges": [
                        {
                          "cursor": "e30x",
                          "node": {
                            "id": 1,
                            "authorId": 1
                          }
                        },
                        {
                          "cursor": "e30y",
                          "node": {
                            "id": 2,
                            "authorId": 1
                          }
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true,
                        "hasPreviousPage": false,
                        "startCursor": "e30x",
                        "endCursor": "e30y"
                      },
                      "totalCount": 4
                    }
                  },
                  {
                    "id": 2,
                    "items": {
                      "nodes": [
                        {
                          "id": 5,
                          "authorId": 2
                        },
                        {
                          "id": 6,
                          "authorId": 2
                        }
                      ],
                      "edges": [
                        {
                          "cursor": "e301",
                          "node": {
                            "id": 5,
                            "authorId": 2
                          }
                        },
                        {
                          "cursor": "e302",
                          "node": {
                            "id": 6,
                            "authorId": 2
                          }
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true,
                        "hasPreviousPage": false,
                        "startCursor": "e301",
                        "endCursor": "e302"
                      },
                      "totalCount": 4
                    },
                    "pagedItems": {
                      "nodes": [
                        {
                          "id": 5,
                          "authorId": 2
                        },
                        {
                          "id": 6,
                          "authorId": 2
                        }
                      ],
                      "edges": [
                        {
                          "cursor": "e301",
                          "node": {
                            "id": 5,
                            "authorId": 2
                          }
                        },
                        {
                          "cursor": "e302",
                          "node": {
                            "id": 6,
                            "authorId": 2
                          }
                        }
                      ],
                      "pageInfo": {
                        "hasNextPage": true,
                        "hasPreviousPage": false,
                        "startCursor": "e301",
                        "endCursor": "e302"
                      },
                      "totalCount": 4
                    }
                  }
                ]
              }
            }
            """);
        Assert.Equal(1, store.Released);
    }

    [Fact]
    public async Task CancelAndDispose_Should_ReleasePage_When_ReaderIsBlockedAndSiblingCompletes()
    {
        // arrange
        var store = await Store.CreateAsync(
            resource.GetConnectionString($"db_{Guid.NewGuid():N}"),
            authorCount: 1,
            itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(store);
        var readerGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        store.ReaderGate = readerGate;
        store.ReaderGateEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        store.SiblingCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        IExecutionResult? result = null;

        try
        {
            // act
            result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                          __typename
                          ... @defer {
                            items(first: 2) { nodes { id } }
                            sibling
                          }
                        }
                        """)
                    .Build(),
                cancellation.Token);
            await using var payloads = result.ExpectResponseStream()
                .ReadResultsAsync()
                .GetAsyncEnumerator(cancellation.Token);
            var hasInitialPayload = await payloads.MoveNextAsync();

            if (hasInitialPayload)
            {
                await payloads.Current.DisposeAsync();
            }

            await store.ReaderGateEntered.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                Xunit.TestContext.Current.CancellationToken);
            await store.SiblingCompleted.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                Xunit.TestContext.Current.CancellationToken);
            var readerWasBlocked = !readerGate.Task.IsCompleted;
            cancellation.Cancel();
            await result.DisposeAsync().AsTask().WaitAsync(
                TimeSpan.FromSeconds(10),
                Xunit.TestContext.Current.CancellationToken);
            await store.WaitForReleaseAsync();

            // assert
            new
            {
                HasInitialPayload = hasInitialPayload,
                SiblingCompleted = store.SiblingCompleted.Task.IsCompleted,
                ReaderWasBlocked = readerWasBlocked,
                Released = store.Released
            }.MatchInlineSnapshot(
                """
                {
                  "HasInitialPayload": true,
                  "SiblingCompleted": true,
                  "ReaderWasBlocked": true,
                  "Released": 1
                }
                """);
            Assert.Equal(1, store.Released);
        }
        finally
        {
            readerGate.TrySetResult(true);

            if (result is not null)
            {
                await result.DisposeAsync();
            }
        }
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(Store store)
        => await new ServiceCollection()
            .AddSingleton(store)
            .AddGraphQLServer()
            .AddStreamPageConnectionTests()
            .AddQueryType<QueryType>()
            .AddType<AuthorType>()
            .AddType<ItemConnectionType>()
            .AddType<PagedItemConnectionType>()
            .AddType<PagedItemEdgeType>()
            .AddQueryContext()
            .AddPagingArguments()
            .ModifyCostOptions(options =>
            {
                options.MaxFieldCost = 20_000;
                options.MaxTypeCost = 20_000;
            })
            .ModifyOptions(options => options.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

    public sealed class Item
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }

        public required string Name { get; set; }
    }

    public sealed class Author
    {
        public int Id { get; set; }

        [UseConnection(IncludeTotalCount = true)]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] StreamItemsByAuthorIdDataLoader dataLoader,
            CancellationToken cancellationToken)
            => await dataLoader.With(arguments).LoadAsync(Id, cancellationToken)
                ?? StreamPage<Item>.Empty;

        [UseConnection(IncludeTotalCount = true)]
        public async Task<PageConnection<Item>> GetPagedItems(
            PagingArguments arguments,
            [Service] PagedItemsByAuthorIdDataLoader dataLoader,
            CancellationToken cancellationToken)
            => await dataLoader.With(arguments).LoadAsync(Id, cancellationToken)
                ?? Page<Item>.Empty;
    }

    public sealed class Query
    {
        [UseConnection(IncludeTotalCount = true)]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] Store store,
            CancellationToken cancellationToken)
        {
            var context = store.CreateContext();
            var page = await context.Items
                .OrderBy(item => item.Id)
                .ToStreamPageAsync(arguments, store.CreateLifetime(context), cancellationToken);
            return page;
        }

        [UseConnection(IncludeTotalCount = true)]
        public async Task<PageConnection<Item>> GetPagedItems(
            int authorId,
            PagingArguments arguments,
            [Service] Store store,
            CancellationToken cancellationToken)
        {
            await using var context = store.CreateContext();
            return await context.Items
                .Where(item => item.AuthorId == authorId)
                .OrderBy(item => item.Id)
                .ToPageAsync(arguments, cancellationToken);
        }

        public List<Author> GetAuthors([Service] Store store)
            => store.AuthorIds.Select(id => new Author { Id = id }).ToList();

        public async Task<string> GetSibling(
            [Service] Store store,
            CancellationToken cancellationToken)
        {
            if (store.ReaderGateEntered is { } readerGateEntered)
            {
                await readerGateEntered.Task.WaitAsync(cancellationToken);
            }

            store.SiblingCompleted?.TrySetResult(true);
            return "sibling";
        }
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(query => query.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>();
            descriptor
                .Field(query => query.GetPagedItems(default, default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<PagedItemConnectionType>>();
        }
    }

    public sealed class AuthorType : ObjectType<Author>
    {
        protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
        {
            descriptor
                .Field(author => author.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>();
            descriptor
                .Field(author => author.GetPagedItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<PagedItemConnectionType>>();
        }
    }

    public sealed class ItemConnectionType : ObjectType<StreamPageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageConnection<Item>> descriptor)
            => descriptor.Name("ItemConnection");
    }

    public sealed class PagedItemConnectionType : ObjectType<PageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<Item>> descriptor)
        {
            descriptor.Name("PagedItemConnection");
            descriptor.Field(connection => connection.Edges)
                .Type<ListType<NonNullType<PagedItemEdgeType>>>();
        }
    }

    public sealed class PagedItemEdgeType : ObjectType<PageEdge<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageEdge<Item>> descriptor)
            => descriptor.Name("PagedItemEdge");
    }

    public sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    public sealed class Store : DbCommandInterceptor
    {
        private readonly TaskCompletionSource<bool> _releasedSignal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _released;

        private Store(string connectionString, int[] authorIds)
        {
            AuthorIds = authorIds;
            _options = new DbContextOptionsBuilder<PagingContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(this)
                .Options;
        }

        private readonly DbContextOptions<PagingContext> _options;

        public int[] AuthorIds { get; }

        public int Released => Volatile.Read(ref _released);

        public TaskCompletionSource<bool>? ReaderGate { get; set; }

        public TaskCompletionSource<bool>? ReaderGateEntered { get; set; }

        public TaskCompletionSource<bool>? SiblingCompleted { get; set; }

        public static async Task<Store> CreateAsync(
            string connectionString,
            int authorCount,
            int itemsPerAuthor)
        {
            var authorIds = Enumerable.Range(1, authorCount).ToArray();
            var store = new Store(connectionString, authorIds);

            await using var context = store.CreateContext();
            await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

            foreach (var authorId in authorIds)
            {
                for (var i = 1; i <= itemsPerAuthor; i++)
                {
                    context.Items.Add(
                        new Item
                        {
                            AuthorId = authorId,
                            Name = $"Item {authorId}-{i}"
                        });
                }
            }

            await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

            return store;
        }

        public PagingContext CreateContext()
            => new(_options);

        public IAsyncDisposable CreateLifetime(PagingContext context)
            => new Lifetime(this, context);

        public Task WaitForReleaseAsync()
            => _releasedSignal.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                Xunit.TestContext.Current.CancellationToken);

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
            => ReaderGate is { } gate
                ? new(new GatedDataReader(result, gate, ReaderGateEntered!))
                : base.ReaderExecutedAsync(command, eventData, result, cancellationToken);

        private sealed class Lifetime(Store store, PagingContext context) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref store._released);
                store._releasedSignal.TrySetResult(true);
                await context.DisposeAsync();
            }
        }
    }

    private sealed class GatedDataReader(
        DbDataReader inner,
        TaskCompletionSource<bool> gate,
        TaskCompletionSource<bool> gateEntered) : DbDataReader
    {
        public override object this[int ordinal] => inner[ordinal];

        public override object this[string name] => inner[name];

        public override int Depth => inner.Depth;

        public override int FieldCount => inner.FieldCount;

        public override bool HasRows => inner.HasRows;

        public override bool IsClosed => inner.IsClosed;

        public override int RecordsAffected => inner.RecordsAffected;

        public override int VisibleFieldCount => inner.VisibleFieldCount;

        public override bool Read() => inner.Read();

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            gateEntered.TrySetResult(true);
            await gate.Task.WaitAsync(cancellationToken);
            return await inner.ReadAsync(cancellationToken);
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

internal static class AuthorItemsDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<StreamPageConnectionExecutionTests.Item>>>
        GetPagedItemsByAuthorIdAsync(
            IReadOnlyList<int> authorIds,
            PagingArguments arguments,
            QueryContext<StreamPageConnectionExecutionTests.Item> query,
            StreamPageConnectionExecutionTests.Store store,
            CancellationToken cancellationToken)
    {
        await using var context = store.CreateContext();
        return await context.Items
            .AsNoTracking()
            .Where(item => authorIds.Contains(item.AuthorId))
            .OrderBy(item => item.Id)
            .With(query.Include(item => item.AuthorId))
            .ToBatchPageAsync(
                item => item.AuthorId,
                arguments,
                cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<int, StreamPage<StreamPageConnectionExecutionTests.Item>>>
        GetStreamItemsByAuthorIdAsync(
            IReadOnlyList<int> authorIds,
            PagingArguments arguments,
            QueryContext<StreamPageConnectionExecutionTests.Item> query,
            StreamPageConnectionExecutionTests.Store store,
            CancellationToken cancellationToken)
    {
        var context = store.CreateContext();
        return await context.Items
            .AsNoTracking()
            .Where(item => authorIds.Contains(item.AuthorId))
            .OrderBy(item => item.Id)
            .With(query.Include(item => item.AuthorId))
            .ToBatchStreamPageAsync(
                item => item.AuthorId,
                arguments,
                store.CreateLifetime(context),
                cancellationToken);
    }
}
