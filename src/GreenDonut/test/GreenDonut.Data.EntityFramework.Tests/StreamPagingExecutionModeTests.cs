using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using XunitTestContext = Xunit.TestContext;

namespace GreenDonut.Data;

// Guards the EF Core 8 fallback: the backward probe and the inlined total count must always run
// as an explicit awaited query, never as a synchronous call.
public class StreamPagingExecutionModeTests
{
    [Fact]
    public async Task ToStreamPageAsync_Should_RunProbeAsync_When_PagingBackward()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(6);
        var cancellationToken = XunitTestContext.Current.CancellationToken;
        var beforeCursor = await database.CreateBeforeCursorAsync(cancellationToken);

        // act
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(last: 2, before: beforeCursor),
            cancellationToken: cancellationToken);
        await DrainAndDisposeAsync(page, cancellationToken);

        // assert
        AssertExecutionMode(
            database,
            netCore8Snapshot:
            """
            [
              "async probe",
              "async rows"
            ]
            """,
            otherSnapshot:
            """
            [
              "async probe"
            ]
            """);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_RunProbeAndCountAsync_When_PagingBackwardWithCount()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(6);
        var cancellationToken = XunitTestContext.Current.CancellationToken;
        var beforeCursor = await database.CreateBeforeCursorAsync(cancellationToken);

        // act
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(last: 2, before: beforeCursor, includeTotalCount: true),
            cancellationToken: cancellationToken);
        await DrainAndDisposeAsync(page, cancellationToken);

        // assert
        AssertExecutionMode(
            database,
            netCore8Snapshot:
            """
            [
              "async probe",
              "async count",
              "async rows"
            ]
            """,
            otherSnapshot:
            """
            [
              "async probe"
            ]
            """);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_RunCountAsync_When_PagingForwardWithCount()
    {
        // arrange
        await using var database = await TestDatabase.CreateAsync(6);
        var cancellationToken = XunitTestContext.Current.CancellationToken;

        // act
        var page = await database.Query.ToStreamPageAsync(
            new PagingArguments(first: 3, includeTotalCount: true),
            cancellationToken: cancellationToken);
        await DrainAndDisposeAsync(page, cancellationToken);

        // assert
        AssertExecutionMode(
            database,
            netCore8Snapshot:
            """
            [
              "async count",
              "async rows"
            ]
            """,
            otherSnapshot:
            """
            [
              "async count"
            ]
            """);
    }

    // otherSnapshot holds the single async row query EF Core 9 and later run; netCore8Snapshot
    // pins EF Core 8's explicit awaited fallback.
    private static void AssertExecutionMode(TestDatabase database, string netCore8Snapshot, string otherSnapshot)
    {
        var records = database.Interceptor.Records;
        var expected = TestEnvironment.TargetFramework == TestEnvironment.NET8_0
            ? netCore8Snapshot
            : otherSnapshot;

        records.MatchInlineSnapshot(expected);
    }

    private static async ValueTask DrainAndDisposeAsync(
        StreamPage<Item> page,
        CancellationToken cancellationToken)
    {
        await foreach (var _ in page.GetEntriesAsync(cancellationToken))
        {
            // draining is the point; the rows themselves are not asserted here.
        }

        await page.DisposeAsync();
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly PagingContext _context;

        private TestDatabase(
            SqliteConnection connection,
            ExecutionModeInterceptor interceptor,
            PagingContext context)
        {
            _connection = connection;
            Interceptor = interceptor;
            _context = context;
        }

        public IQueryable<Item> Query => _context.Items.OrderBy(t => t.Id);

        public ExecutionModeInterceptor Interceptor { get; }

        public static async Task<TestDatabase> CreateAsync(int itemCount)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(XunitTestContext.Current.CancellationToken);

            var interceptor = new ExecutionModeInterceptor();
            var options = new DbContextOptionsBuilder<PagingContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;
            var context = new PagingContext(options);
            await context.Database.EnsureCreatedAsync(XunitTestContext.Current.CancellationToken);

            for (var i = 1; i <= itemCount; i++)
            {
                context.Items.Add(new Item { Id = i });
            }

            await context.SaveChangesAsync(XunitTestContext.Current.CancellationToken);
            interceptor.Reset();
            return new TestDatabase(connection, interceptor, context);
        }

        // The last entry of a full first:N page yields a cursor beyond the seeded data, so a
        // backward page fetched with it carries the full set as candidates.
        public async ValueTask<string> CreateBeforeCursorAsync(CancellationToken cancellationToken)
        {
            var fullPage = await Query.ToStreamPageAsync(
                new PagingArguments(first: 100),
                cancellationToken: cancellationToken);
            List<PageEntry<Item>> entries = [];

            await foreach (var entry in fullPage.GetEntriesAsync(cancellationToken))
            {
                entries.Add(entry);
            }

            await fullPage.DisposeAsync();
            var cursor = fullPage.CreateCursor(entries[^1]);
            Interceptor.Reset();
            return cursor;
        }

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
    }

    /// <summary>
    /// Records every command execution EF Core 8's fallback might issue, distinguishing a
    /// synchronous call from its awaited counterpart, alongside the kind of command it ran.
    /// </summary>
    private sealed class ExecutionModeInterceptor : DbCommandInterceptor
    {
        private readonly List<string> _records = [];

        public IReadOnlyList<string> Records => _records;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command, isAsync: false);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command, isAsync: true);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            Record(command, isAsync: false);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Record(command, isAsync: true);
            return ValueTask.FromResult(result);
        }

        public void Reset() => _records.Clear();

        private void Record(DbCommand command, bool isAsync)
        {
            var kind = command.CommandText.Contains("EXISTS", StringComparison.OrdinalIgnoreCase)
                ? "probe"
                : command.CommandText.Contains("COUNT(", StringComparison.OrdinalIgnoreCase)
                    ? "count"
                    : "rows";
            _records.Add($"{(isAsync ? "async" : "sync")} {kind}");
        }
    }
}
