#if NET9_0_OR_GREATER
using System.Collections;
using System.Data;
using System.Data.Common;
using CookieCrumble.Resources;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Pagination;

public class StreamPageConnectionLifetimeTests
{
    [Fact]
    public async Task Execute_Should_RunOnlyTheCountQuery_When_OnlyTotalCountIsSelected()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { totalCount } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "totalCount": 5
                }
              }
            }
            """);
        probe.Commands.MatchInlineSnapshot(
            """
            [
              "SELECT COUNT(*)\nFROM \"Items\" AS \"i\""
            ]
            """);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Execute_Should_ReturnRowsAndTotalCount_When_TotalCountAndNodesAreSelected()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ items(first: 2) { totalCount nodes { id } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": {
                  "totalCount": 5,
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
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Execute_Should_ShareOnePage_When_NodesEdgesAndPageInfoAreSelected()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        await using var result = await executor.ExecuteAsync(
            """
            {
              items(first: 2) {
                nodes { id }
                edges { cursor node { id } }
                pageInfo { hasNextPage hasPreviousPage startCursor endCursor }
                totalCount
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
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
                  "pageInfo": {
                    "hasNextPage": true,
                    "hasPreviousPage": false,
                    "startCursor": "e30x",
                    "endCursor": "e30y"
                  },
                  "totalCount": 5
                }
              }
            }
            """);
        Assert.Single(probe.Commands);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageWithTheStream_When_NodesAreDeferred()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        List<string> payloads = [];
        List<int> releasedAfterPayload = [];
        probe.NodesGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { totalCount ... @defer { nodes { id } } } }",
            TestContext.Current.CancellationToken);

        await foreach (var payload in result.ExpectResponseStream().ReadResultsAsync())
        {
            payloads.Add(payload.ToJson());
            await payload.DisposeAsync();
            releasedAfterPayload.Add(probe.Released);
            probe.NodesGate.TrySetResult();
        }

        await result.DisposeAsync();

        // assert
        payloads.MatchInlineSnapshots(
        [
            """
            {
              "data": {
                "items": {
                  "totalCount": 5
                }
              },
              "pending": [
                {
                  "id": "2",
                  "path": [
                    "items"
                  ]
                }
              ],
              "hasNext": true
            }
            """,
            """
            {
              "incremental": [
                {
                  "id": "2",
                  "data": {
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
              ],
              "completed": [
                {
                  "id": "2"
                }
              ],
              "hasNext": false
            }
            """
        ]);
        Assert.Equal(0, releasedAfterPayload[0]);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_ThePageIsAbandonedBeforeCompletion()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { pageInfo { startCursor } } }",
            TestContext.Current.CancellationToken);
        var releasedBeforeDispose = probe.Released;
        await result.DisposeAsync();
        await result.DisposeAsync();

        // assert
        Assert.Equal(0, releasedBeforeDispose);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_TheConnectionIsDerived()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        var result = await executor.ExecuteAsync(
            "{ derivedItems(first: 2) { pageInfo { startCursor } } }",
            TestContext.Current.CancellationToken);
        await result.DisposeAsync();

        // assert
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_ThePageIsCompleted()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } } }",
            TestContext.Current.CancellationToken);
        var releasedBeforeDispose = probe.Released;
        await result.DisposeAsync();

        // assert
        Assert.Equal(1, releasedBeforeDispose);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_AnotherFieldFaultsTheRequest()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { pageInfo { startCursor } } fail }",
            TestContext.Current.CancellationToken);
        var errors = result.ExpectOperationResult().Errors.Select(t => t.Message).ToArray();
        await result.DisposeAsync();

        // assert
        Assert.Equal(["The fail field faulted."], errors);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Execute_Should_ReleaseTheLifetime_When_OpeningTheRowReaderFaults()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        probe.FailRowReader = true;

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } } }",
            TestContext.Current.CancellationToken);
        var errors = result.ExpectOperationResult().Errors.Select(t => t.Message).ToArray();
        await result.DisposeAsync();

        // assert
        Assert.Equal(["Unexpected Execution Error"], errors);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_ReadingRowsFaultsAfterConnectionCreation()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        probe.FailAfterFirstRow = true;

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } } }",
            TestContext.Current.CancellationToken);
        var errors = result.ExpectOperationResult().Errors.Select(t => t.Message).ToArray();
        await result.DisposeAsync();

        // assert
        Assert.Equal(["Unexpected Execution Error"], errors);
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_TheRequestIsCancelledAfterThePageWasCreated()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        using var cancellation = new CancellationTokenSource();
        probe.CancelAfterCreate = cancellation;

        // act
        var result = await executor.ExecuteAsync(
            "{ items(first: 2) { nodes { id } } }",
            cancellation.Token);
        await result.DisposeAsync();

        // assert
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_TheRequestIsCancelledWhileTheDeferredBranchRuns()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        using var cancellation = new CancellationTokenSource();
        probe.CancelOnNodes = cancellation;

        // act
        var result = await executor.ExecuteAsync(
            "{ __typename ... @defer { items(first: 2) { nodes { id } } } }",
            cancellation.Token);
        await result.DisposeAsync();
        await probe.WaitForReleaseAsync();

        // assert
        Assert.Equal(1, probe.Released);
    }

    [Fact]
    public async Task Dispose_Should_ReleaseThePageOnce_When_TheStreamIsDisposedBeforeTheDeferredPayloadArrives()
    {
        // arrange
        await using var probe = await Probe.CreateSqliteAsync(authorCount: 1, itemsPerAuthor: 5);
        var executor = await CreateExecutorAsync(probe);
        probe.NodesGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // act
        var result = await executor.ExecuteAsync(
            "{ __typename ... @defer { items(first: 2) { pageInfo { startCursor } } } }",
            TestContext.Current.CancellationToken);

        await foreach (var payload in result.ExpectResponseStream().ReadResultsAsync())
        {
            await payload.DisposeAsync();
            break;
        }

        await result.DisposeAsync();
        probe.NodesGate.TrySetResult();
        await probe.WaitForReleaseAsync();

        // assert
        Assert.Equal(1, probe.Released);
    }

    internal static async Task<IRequestExecutor> CreateExecutorAsync(Probe probe)
        => await new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddType<AuthorType>()
            .AddType<ItemConnectionType>()
            .AddDataLoader<ItemsByAuthorDataLoader>()
            .AddPagingArguments()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public sealed class Item
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }
    }

    public sealed class Author
    {
        public int Id { get; set; }

        [UseConnection]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] ItemsByAuthorDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            var page = await dataLoader.With(arguments).LoadAsync(Id, cancellationToken);

            return page!;
        }
    }

    public sealed class Query
    {
        [UseConnection(IncludeTotalCount = true)]
        public async Task<StreamPageConnection<Item>> GetItems(
            PagingArguments arguments,
            [Service] Probe probe,
            CancellationToken cancellationToken)
        {
            var context = probe.CreateContext();
            var page = await context.Items
                .OrderBy(t => t.Id)
                .ToStreamPageAsync(arguments, probe.CreateLifetime(context), cancellationToken);

            probe.CancelAfterCreate?.Cancel();

            return page;
        }

        [UseConnection]
        public async Task<DerivedItemConnection> GetDerivedItems(
            PagingArguments arguments,
            [Service] Probe probe,
            CancellationToken cancellationToken)
        {
            var context = probe.CreateContext();
            var page = await context.Items
                .OrderBy(t => t.Id)
                .ToStreamPageAsync(arguments, probe.CreateLifetime(context), cancellationToken);

            return new DerivedItemConnection(page);
        }

        public List<Author> GetAuthors([Service] Probe probe)
            => probe.Authors.Select(id => new Author { Id = id }).ToList();

        public int GetFail()
            => throw new GraphQLException("The fail field faulted.");
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(t => t.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>();
            descriptor
                .Field(t => t.GetDerivedItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>();
        }
    }

    public sealed class AuthorType : ObjectType<Author>
    {
        protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
        {
            descriptor
                .Field(t => t.GetItems(default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<ItemConnectionType>>();
        }
    }

    public sealed class DerivedItemConnection(StreamPage<Item> page) : StreamPageConnection<Item>(page);

    public sealed class ItemConnectionType : ObjectType<StreamPageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageConnection<Item>> descriptor)
        {
            descriptor.Name("ItemConnection");
            descriptor.Field(t => t.GetNodesAsync(default)).Use(GateMiddleware);
            descriptor.Field(t => t.GetEdgesAsync(default)).Type<ListType<NonNullType<ItemEdgeType>>>();
            descriptor
                .Field(t => t.PageInfo)
                .Type<NonNullType<ItemPageInfoType>>()
                .Use(GateMiddleware);
        }

        private static FieldDelegate GateMiddleware(FieldDelegate next)
            => async context =>
            {
                var probe = context.Service<Probe>();
                probe.CancelOnNodes?.Cancel();

                if (probe.NodesGate is { } gate)
                {
                    await gate.Task;
                }

                await next(context);
            };
    }

    public sealed class ItemEdgeType : ObjectType<StreamPageEdge<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageEdge<Item>> descriptor)
            => descriptor.Name("ItemEdge");
    }

    public sealed class ItemPageInfoType : ObjectType<StreamPageInfo<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageInfo<Item>> descriptor)
            => descriptor.Name("ItemPageInfo");
    }

    public sealed class ItemsByAuthorDataLoader(
        Probe probe,
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
            var dbContext = probe.CreateContext();

            return await dbContext.Items
                .Where(t => keys.Contains(t.AuthorId))
                .OrderBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.AuthorId,
                    arguments,
                    probe.CreateLifetime(dbContext),
                    cancellationToken);
        }
    }

    public sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    public sealed class Probe : DbCommandInterceptor, IAsyncDisposable
    {
        private readonly SqliteConnection? _connection;
        private readonly DbContextOptions<PagingContext> _options;
        private readonly TaskCompletionSource _releasedSignal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _created;
        private int _released;

        private Probe(
            Func<DbContextOptionsBuilder<PagingContext>, DbContextOptionsBuilder<PagingContext>> configure,
            SqliteConnection? connection,
            int[] authors)
        {
            _connection = connection;
            Authors = authors;
            _options = configure(new DbContextOptionsBuilder<PagingContext>())
                .AddInterceptors(this)
                .Options;
        }

        public int[] Authors { get; }

        public List<string> Commands { get; } = [];

        public int Created => Volatile.Read(ref _created);

        public int Released => Volatile.Read(ref _released);

        public CancellationTokenSource? CancelAfterCreate { get; set; }

        public CancellationTokenSource? CancelOnNodes { get; set; }

        public TaskCompletionSource? NodesGate { get; set; }

        public bool FailRowReader { get; set; }

        public bool FailAfterFirstRow { get; set; }

        public static async Task<Probe> CreateSqliteAsync(int authorCount, int itemsPerAuthor)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            return await SeedAsync(b => b.UseSqlite(connection), connection, authorCount, itemsPerAuthor);
        }

        public static Task<Probe> CreatePostgresAsync(string connectionString, int authorCount, int itemsPerAuthor)
            => SeedAsync(b => b.UseNpgsql(connectionString), null, authorCount, itemsPerAuthor);

        private static async Task<Probe> SeedAsync(
            Func<DbContextOptionsBuilder<PagingContext>, DbContextOptionsBuilder<PagingContext>> configure,
            SqliteConnection? connection,
            int authorCount,
            int itemsPerAuthor)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var authors = Enumerable.Range(1, authorCount).ToArray();
            var probe = new Probe(configure, connection, authors);

            await using (var context = new PagingContext(probe._options))
            {
                await context.Database.EnsureCreatedAsync(cancellationToken);

                foreach (var authorId in authors)
                {
                    for (var i = 0; i < itemsPerAuthor; i++)
                    {
                        context.Items.Add(new Item { AuthorId = authorId });
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
            }

            probe.Commands.Clear();

            return probe;
        }

        public PagingContext CreateContext()
        {
            Interlocked.Increment(ref _created);
            return new PagingContext(_options);
        }

        public Task WaitForReleaseAsync()
            => _releasedSignal.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        public IAsyncDisposable CreateLifetime(PagingContext context) => new Lifetime(this, context);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            lock (Commands)
            {
                Commands.Add(command.CommandText);
            }

            if (FailRowReader)
            {
                throw new InvalidOperationException("The row reader faulted.");
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
            => FailAfterFirstRow
                ? new(new FaultingDataReader(result))
                : base.ReaderExecutedAsync(command, eventData, result, cancellationToken);

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }

        private sealed class FaultingDataReader(DbDataReader inner) : DbDataReader
        {
            private bool _firstRead = true;

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
                ThrowIfAlreadyRead();
                return inner.Read();
            }

            public override Task<bool> ReadAsync(CancellationToken cancellationToken)
            {
                ThrowIfAlreadyRead();
                return inner.ReadAsync(cancellationToken);
            }

            public override bool NextResult() => inner.NextResult();

            public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
                => inner.NextResultAsync(cancellationToken);

            public override bool GetBoolean(int ordinal) => inner.GetBoolean(ordinal);

            public override byte GetByte(int ordinal) => inner.GetByte(ordinal);

            public override long GetBytes(
                int ordinal,
                long dataOffset,
                byte[]? buffer,
                int bufferOffset,
                int length)
                => inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

            public override char GetChar(int ordinal) => inner.GetChar(ordinal);

            public override long GetChars(
                int ordinal,
                long dataOffset,
                char[]? buffer,
                int bufferOffset,
                int length)
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

            private void ThrowIfAlreadyRead()
            {
                if (!_firstRead)
                {
                    throw new InvalidOperationException("The row reader faulted.");
                }

                _firstRead = false;
            }
        }

        private sealed class Lifetime(Probe probe, PagingContext context) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref probe._released);
                probe._releasedSignal.TrySetResult();
                await context.DisposeAsync();
            }
        }
    }
}

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPageConnectionBatchLifetimeTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Dispose_Should_ReleaseBatchPagesTogether_When_PagesShareOnePump()
    {
        // arrange
        await using var probe = await StreamPageConnectionLifetimeTests.Probe.CreatePostgresAsync(
            resource.GetConnectionString($"db_{Guid.NewGuid():N}"),
            authorCount: 2,
            itemsPerAuthor: 4);
        var executor = await StreamPageConnectionLifetimeTests.CreateExecutorAsync(probe);

        // act
        var result = await executor.ExecuteAsync(
            "{ authors { id items(first: 2) { pageInfo { startCursor } } } }",
            TestContext.Current.CancellationToken);
        var errorCount = result.ExpectOperationResult().Errors.Count;
        var releasedBeforeDispose = probe.Released;
        await result.DisposeAsync();

        // assert
        Assert.Equal(0, errorCount);
        Assert.Equal(1, probe.Created);
        Assert.Equal(0, releasedBeforeDispose);
        Assert.Equal(1, probe.Released);
    }
}
#endif
