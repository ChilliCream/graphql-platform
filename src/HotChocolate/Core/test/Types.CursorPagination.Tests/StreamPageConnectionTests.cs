using GreenDonut.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.Pagination;

public class StreamPageConnectionTests
{
    [Fact]
    public async Task ImplicitConversion_Should_WrapPage_When_ConvertingStreamPageToConnection()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2, includeTotalCount: true),
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        StreamPageConnection<Item> connection = page;

        // assert
        Assert.Equal(5, await connection.GetTotalCountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ImplicitConversion_Should_ThrowArgumentNullException_When_PageIsNull()
    {
        // arrange
        StreamPage<string>? page = null;

        // act
        StreamPageConnection<string> Convert() => page!;

        // assert
        Assert.Throws<ArgumentNullException>(Convert);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_MaxRelativeCursorCountIsNegative()
    {
        // arrange
        var page = StreamPage<string>.Empty;

        // act
        StreamPageConnection<string> Create() => new(page, maxRelativeCursorCount: -1);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Create);
    }

    [Fact]
    public async Task GetEdgesAsync_Should_PreserveEntryCursors_When_PageIsEnumerated()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 3),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);

        // act
        List<object> edges = [];
        await foreach (var edge in connection.GetEdgesAsync(TestContext.Current.CancellationToken)!)
        {
            edges.Add(new { edge.Node.Id, edge.Cursor });
        }

        // assert
        edges.MatchInlineSnapshot(
            """
            [
              {
                "Id": 1,
                "Cursor": "e30x"
              },
              {
                "Id": 2,
                "Cursor": "e30y"
              },
              {
                "Id": 3,
                "Cursor": "e30z"
              }
            ]
            """);
    }

    [Fact]
    public async Task GetEdgesAsync_Should_ReplayAllEdges_When_EnumeratedTwice()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 3),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);

        // act
        var first = await CollectIdsAsync(connection);
        var second = await CollectIdsAsync(connection);

        // assert
        Assert.Equal([1, 2, 3], first);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task PageInfo_Should_ResolveFlagsAndCursors_When_RelativeCursorsAreEnabled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(10);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2, includeTotalCount: true) { EnableRelativeCursors = true },
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page, maxRelativeCursorCount: 2);

        // act
        var cancellationToken = TestContext.Current.CancellationToken;
        var pageInfo = connection.PageInfo;
        var snapshot = new
        {
            HasNextPage = await pageInfo.HasNextPageAsync(cancellationToken),
            HasPreviousPage = await pageInfo.HasPreviousPageAsync(cancellationToken),
            StartCursor = await pageInfo.GetStartCursorAsync(cancellationToken),
            EndCursor = await pageInfo.GetEndCursorAsync(cancellationToken),
            ForwardCursors = await pageInfo.GetForwardCursorsAsync(cancellationToken),
            BackwardCursors = await pageInfo.GetBackwardCursorsAsync(cancellationToken)
        };

        // assert
        snapshot.MatchInlineSnapshot(
            """
            {
              "HasNextPage": true,
              "HasPreviousPage": false,
              "StartCursor": "e30x",
              "EndCursor": "e30y",
              "ForwardCursors": [
                {
                  "Cursor": "ezB8MXwxMH0y",
                  "Page": 2
                },
                {
                  "Cursor": "ezF8MXwxMH0y",
                  "Page": 3
                }
              ],
              "BackwardCursors": []
            }
            """);
    }

    [Fact]
    public async Task PageInfo_Should_ResolveBackwardCursors_When_PageIsNotTheFirstPage()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(10);
        var firstPage = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2, includeTotalCount: true) { EnableRelativeCursors = true },
            cancellationToken: TestContext.Current.CancellationToken);
        var lastEntry = await GetLastEntryAndDisposeAsync(firstPage);
        var cursor = firstPage.CreateCursor(lastEntry, 0);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2, after: cursor) { EnableRelativeCursors = true },
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);

        // act
        var cancellationToken = TestContext.Current.CancellationToken;
        var pageInfo = connection.PageInfo;
        var snapshot = new
        {
            HasNextPage = await pageInfo.HasNextPageAsync(cancellationToken),
            HasPreviousPage = await pageInfo.HasPreviousPageAsync(cancellationToken),
            BackwardCursors = await pageInfo.GetBackwardCursorsAsync(cancellationToken)
        };

        // assert
        snapshot.MatchInlineSnapshot(
            """
            {
              "HasNextPage": true,
              "HasPreviousPage": true,
              "BackwardCursors": [
                {
                  "Cursor": "ezB8MnwxMH0z",
                  "Page": 1
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task GetTotalCountAsync_Should_ReturnNull_When_CountWasNotRequested()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);

        // act
        var totalCount = await connection.GetTotalCountAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Null(totalCount);
    }

    [Fact]
    public async Task GetNodesAsync_Should_ThrowOperationCanceledException_When_TokenIsCancelled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        async Task Enumerate()
        {
            await foreach (var _ in connection.GetNodesAsync(cts.Token)!)
            {
            }
        }

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(Enumerate);
    }

    [Fact]
    public async Task GetEdgesAsync_Should_ThrowOperationCanceledException_When_TokenIsCancelled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        async Task Enumerate()
        {
            await foreach (var _ in connection.GetEdgesAsync(cts.Token)!)
            {
            }
        }

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(Enumerate);
    }

    [Fact]
    public async Task PageInfo_Should_ThrowOperationCanceledException_When_ForwardCursorsTokenIsCancelled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2, includeTotalCount: true) { EnableRelativeCursors = true },
            cancellationToken: TestContext.Current.CancellationToken);
        var pageInfo = new StreamPageConnection<Item>(page).PageInfo;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        async Task Resolve() => await pageInfo.GetForwardCursorsAsync(cts.Token);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(Resolve);
    }

    [Fact]
    public async Task PageInfo_Should_ThrowOperationCanceledException_When_EndCursorTokenIsCancelled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        var pageInfo = new StreamPageConnection<Item>(page).PageInfo;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        async Task Resolve() => await pageInfo.GetEndCursorAsync(cts.Token);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(Resolve);
    }

    [Fact]
    public async Task PageInfo_Should_ThrowOperationCanceledException_When_HasNextPageTokenIsCancelled()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 2),
            cancellationToken: TestContext.Current.CancellationToken);
        var pageInfo = new StreamPageConnection<Item>(page).PageInfo;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        async Task Resolve() => await pageInfo.HasNextPageAsync(cts.Token);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(Resolve);
    }

    [Fact]
    public async Task GetNodesAsync_Should_StreamNodes_When_PageIsEnumerated()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(5);
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 3),
            cancellationToken: TestContext.Current.CancellationToken);
        var connection = new StreamPageConnection<Item>(page);

        // act
        List<int> ids = [];
        await foreach (var node in connection.GetNodesAsync(TestContext.Current.CancellationToken)!)
        {
            ids.Add(node.Id);
        }

        // assert
        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public async Task Connection_Should_BeEmpty_When_PageIsEmpty()
    {
        // arrange
        StreamPageConnection<string> connection = StreamPage<string>.Empty;

        // act
        List<object?> edges = [];
        await foreach (var edge in connection.GetEdgesAsync(TestContext.Current.CancellationToken)!)
        {
            edges.Add(edge);
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var pageInfo = connection.PageInfo;
        var snapshot = new
        {
            Edges = edges,
            TotalCount = await connection.GetTotalCountAsync(TestContext.Current.CancellationToken),
            HasNextPage = await pageInfo.HasNextPageAsync(cancellationToken),
            HasPreviousPage = await pageInfo.HasPreviousPageAsync(cancellationToken),
            StartCursor = await pageInfo.GetStartCursorAsync(cancellationToken),
            EndCursor = await pageInfo.GetEndCursorAsync(cancellationToken),
            ForwardCursors = await pageInfo.GetForwardCursorsAsync(cancellationToken),
            BackwardCursors = await pageInfo.GetBackwardCursorsAsync(cancellationToken)
        };

        // assert
        snapshot.MatchInlineSnapshot(
            """
            {
              "Edges": [],
              "TotalCount": 0,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "StartCursor": null,
              "EndCursor": null,
              "ForwardCursors": [],
              "BackwardCursors": []
            }
            """);
    }

    private static async Task<PageEntry<Item>> GetLastEntryAndDisposeAsync(StreamPage<Item> page)
    {
        PageEntry<Item>? last = null;

        await foreach (var entry in page.GetEntriesAsync(TestContext.Current.CancellationToken))
        {
            last = entry;
        }

        await page.DisposeAsync();

        return last!.Value;
    }

    private static async Task<List<int>> CollectIdsAsync(StreamPageConnection<Item> connection)
    {
        List<int> ids = [];

        await foreach (var edge in connection.GetEdgesAsync(TestContext.Current.CancellationToken)!)
        {
            ids.Add(edge.Node.Id);
        }

        return ids;
    }

    public sealed class Item
    {
        public int Id { get; set; }
    }

    private sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly PagingContext _context;

        private TestDatabase(SqliteConnection connection, PagingContext context)
        {
            _connection = connection;
            _context = context;
        }

        public IQueryable<Item> Query => _context.Items.OrderBy(t => t.Id);

        public static async Task<TestDatabase> CreateAsync(int itemCount)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(cancellationToken);

            var options = new DbContextOptionsBuilder<PagingContext>()
                .UseSqlite(connection)
                .Options;
            var context = new PagingContext(options);
            await context.Database.EnsureCreatedAsync(cancellationToken);

            for (var i = 1; i <= itemCount; i++)
            {
                context.Items.Add(new Item { Id = i });
            }

            await context.SaveChangesAsync(cancellationToken);

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
