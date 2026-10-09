using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XunitTestContext = Xunit.TestContext;

namespace GreenDonut.Data;

public class PagingTotalCountTests
{
    [Fact]
    public async Task ToPageAsync_Should_ReturnZeroTotalCount_When_FilteredDataSetIsEmpty()
    {
        await using var database = await TestDatabase.CreateAsync(2);

        var page = await database.Query.Where(t => t.Id < 0).ToPageAsync(
            new PagingArguments(first: 2, includeTotalCount: true),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 0,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 2
            }
            """);
    }

    [Fact]
    public async Task ToPageAsync_Should_PreserveCursorTotalCount_When_CountIsNotRequested()
    {
        await using var database = await TestDatabase.CreateAsync(2);
        var firstPage = await database.Query.ToPageAsync(
            new PagingArguments(first: 2) { EnableRelativeCursors = true },
            XunitTestContext.Current.CancellationToken);
        var cursor = firstPage.CreateCursor(firstPage.Last!.Value, 0);
        database.ResetCountQueryCount();

        var page = await database.Query.ToPageAsync(
            new PagingArguments(first: 2, after: cursor),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 2,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 0
            }
            """);
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnDataSetCount_When_PageAfterLastItemIsEmpty()
    {
        await using var database = await TestDatabase.CreateAsync(2);
        var firstPage = await database.Query.ToPageAsync(
            new PagingArguments(first: 2),
            XunitTestContext.Current.CancellationToken);
        var cursor = firstPage.CreateCursor(firstPage.Last!.Value);
        database.ResetCountQueryCount();

        var page = await database.Query.ToPageAsync(
            new PagingArguments(first: 2, after: cursor, includeTotalCount: true),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 2,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 2
            }
            """);
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnDataSetCount_When_PageBeforeFirstItemIsEmpty()
    {
        await using var database = await TestDatabase.CreateAsync(2);
        var firstPage = await database.Query.ToPageAsync(
            new PagingArguments(first: 2),
            XunitTestContext.Current.CancellationToken);
        var cursor = firstPage.CreateCursor(firstPage.First!.Value);
        database.ResetCountQueryCount();

        var page = await database.Query.ToPageAsync(
            new PagingArguments(last: 2, before: cursor, includeTotalCount: true),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 2,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 2
            }
            """);
    }

    [Fact]
    public async Task ToPageAsync_Should_LeaveTotalCountUnknown_When_CountIsNotRequested()
    {
        await using var database = await TestDatabase.CreateAsync(0);

        var page = await database.Query.ToPageAsync(
            new PagingArguments(first: 2),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": null,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 0
            }
            """);
    }

    [Fact]
    public async Task ToPageAsync_Should_UseCombinedCountQuery_When_PageIsNotEmpty()
    {
        await using var database = await TestDatabase.CreateAsync(2);

        var page = await database.Query.ToPageAsync(
            new PagingArguments(first: 1, includeTotalCount: true),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(page, database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 2,
              "HasNextPage": true,
              "HasPreviousPage": false,
              "Items": [
                1
              ],
              "CountQueryCount": 1
            }
            """);
    }

    [Fact]
    public async Task ToBatchPageAsync_Should_PreserveTotalCount_When_PageAfterLastItemIsEmpty()
    {
        await using var database = await TestDatabase.CreateAsync(2);
        var firstPage = await database.Query.ToPageAsync(
            new PagingArguments(first: 2),
            XunitTestContext.Current.CancellationToken);
        var cursor = firstPage.CreateCursor(firstPage.Last!.Value);
        database.ResetCountQueryCount();

        var pages = await database.Query.ToBatchPageAsync(
            t => t.GroupId,
            new PagingArguments(first: 2, after: cursor, includeTotalCount: true),
            XunitTestContext.Current.CancellationToken);

        CreateSnapshot(pages[1], database.CountQueryCount).MatchInlineSnapshot(
            """
            {
              "TotalCount": 2,
              "HasNextPage": false,
              "HasPreviousPage": false,
              "Items": [],
              "CountQueryCount": 1
            }
            """);
    }

    private static object CreateSnapshot(Page<Item> page, int countQueryCount)
        => new
        {
            page.TotalCount,
            page.HasNextPage,
            page.HasPreviousPage,
            Items = page.Select(t => t.Id).ToArray(),
            CountQueryCount = countQueryCount
        };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly CommandCounter _commandCounter;
        private readonly PagingContext _context;

        private TestDatabase(
            SqliteConnection connection,
            CommandCounter commandCounter,
            PagingContext context)
        {
            _connection = connection;
            _commandCounter = commandCounter;
            _context = context;
        }

        public IQueryable<Item> Query => _context.Items.OrderBy(t => t.Id);

        public int CountQueryCount => _commandCounter.Count;

        public static async Task<TestDatabase> CreateAsync(int itemCount)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(XunitTestContext.Current.CancellationToken);

            var commandCounter = new CommandCounter();
            var options = new DbContextOptionsBuilder<PagingContext>()
                .UseSqlite(connection)
                .AddInterceptors(commandCounter)
                .Options;
            var context = new PagingContext(options);
            await context.Database.EnsureCreatedAsync(XunitTestContext.Current.CancellationToken);

            for (var i = 1; i <= itemCount; i++)
            {
                context.Items.Add(new Item { Id = i, GroupId = 1 });
            }

            await context.SaveChangesAsync(XunitTestContext.Current.CancellationToken);
            commandCounter.Reset();
            return new TestDatabase(connection, commandCounter, context);
        }

        public void ResetCountQueryCount() => _commandCounter.Reset();

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    private sealed class Item
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CountQuery(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountQuery(command);
            return ValueTask.FromResult(result);
        }

        public void Reset() => Count = 0;

        private void CountQuery(DbCommand command)
        {
            if (command.CommandText.Contains("COUNT(", StringComparison.OrdinalIgnoreCase))
            {
                Count++;
            }
        }
    }
}
