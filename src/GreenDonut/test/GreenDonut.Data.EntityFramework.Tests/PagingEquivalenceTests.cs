#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

// Verifies that ToStreamPageAsync and ToBatchStreamPageAsync return the same items, cursors,
// flags, index and total count as ToPageAsync and ToBatchPageAsync for the same arguments.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingEquivalenceTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    public static TheoryData<string> Scenarios()
        => new()
        {
            "First",
            "FirstAfter",
            "Last",
            "LastBefore",
            "RelativeAfter",
            "RelativeAfterOffset",
            "RelativeBeforeCountsOn",
            "EndCursor",
            "CountsOn",
            "CountsOff"
        };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task ToStreamPageAsync_Should_MatchToPageAsync_When_Scenario(string scenario)
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 10));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = await BuildArgumentsAsync(scenario, connectionString, cancellationToken);

        // act
        await using var pageContext = new SequentialItemContext(connectionString);
        var page = await KeyASource(pageContext).ToPageAsync(arguments, cancellationToken);

        await using var streamContext = new SequentialItemContext(connectionString);
        var streamPage = await KeyASource(streamContext)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // assert
        await AssertEquivalentAsync(page, streamPage, t => t.Name, cancellationToken);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task ToBatchStreamPageAsync_Should_MatchToBatchPageAsync_When_Scenario(string scenario)
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString, ("A", 10), ("B", 4));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = await BuildArgumentsAsync(scenario, connectionString, cancellationToken);

        // act
        await using var pageContext = new SequentialItemContext(connectionString);
        var pages = await pageContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        await using var streamContext = new SequentialItemContext(connectionString);
        var streamPages = await streamContext.Items
            .Where(t => new[] { "A", "B" }.Contains(t.GroupKey))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupKey, arguments, cancellationToken: cancellationToken);

        // assert
        Assert.Equal(pages.Keys.OrderBy(k => k), streamPages.Keys.OrderBy(k => k));

        foreach (var key in pages.Keys)
        {
            await AssertEquivalentAsync(pages[key], streamPages[key], t => t.Name, cancellationToken);
        }
    }

    private static IQueryable<SequentialItem> KeyASource(SequentialItemContext context)
        => context.Items.Where(t => t.GroupKey == "A").OrderBy(t => t.Name).ThenBy(t => t.Id);

    // Builds the paging arguments for a scenario from a real page fetched against the same
    // seeded data, so cursors are never hand-written literals.
    private static async Task<PagingArguments> BuildArgumentsAsync(
        string scenario,
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var seedContext = new SequentialItemContext(connectionString);
        var source = KeyASource(seedContext);

        switch (scenario)
        {
            case "First":
                return new PagingArguments(3);

            case "FirstAfter":
            {
                var seedPage = await source.ToPageAsync(new PagingArguments(10), cancellationToken);
                return new PagingArguments(3) { After = seedPage.CreateCursor(seedPage.Entries[1]) };
            }

            case "Last":
                return new PagingArguments(last: 3);

            case "LastBefore":
            {
                var seedPage = await source.ToPageAsync(new PagingArguments(10), cancellationToken);
                return new PagingArguments(last: 3) { Before = seedPage.CreateCursor(seedPage.Entries[8]) };
            }

            case "RelativeAfter":
            {
                var relativeArguments = new PagingArguments(3) { EnableRelativeCursors = true };
                var seedPage = await source.ToPageAsync(relativeArguments, cancellationToken);
                return relativeArguments with { After = seedPage.CreateCursor(seedPage.Entries[^1], 0) };
            }

            case "RelativeAfterOffset":
            {
                var relativeArguments = new PagingArguments(3) { EnableRelativeCursors = true };
                var seedPage = await source.ToPageAsync(relativeArguments, cancellationToken);
                return relativeArguments with { After = seedPage.CreateCursor(seedPage.Entries[^1], 1) };
            }

            case "RelativeBeforeCountsOn":
            {
                var relativeArguments = new PagingArguments(last: 3)
                {
                    EnableRelativeCursors = true,
                    IncludeTotalCount = true
                };
                var seedPage = await source.ToPageAsync(relativeArguments, cancellationToken);
                return relativeArguments with { Before = seedPage.CreateCursor(seedPage.Entries[0], -1) };
            }

            case "EndCursor":
                return new PagingArguments(last: 2) { Before = CursorFormatter.FormatEndCursor(-1, 10) };

            case "CountsOn":
                return new PagingArguments(3) { IncludeTotalCount = true };

            case "CountsOff":
                return new PagingArguments(3) { IncludeTotalCount = false };

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
        }
    }

    // Asserts that a stream page matches its non-streaming counterpart: same items in order,
    // byte-identical cursors, the same flags, index and total count.
    private static async Task AssertEquivalentAsync<T>(
        Page<T> expected,
        StreamPage<T> actual,
        Func<T, string> nameSelector,
        CancellationToken cancellationToken)
    {
        var actualEntries = await EntriesAsync(actual);
        Func<PageEntry<T>, string> expectedCursor = expected.CreateCursor;
        Func<PageEntry<T>, string> actualCursor = actual.CreateCursor;

        Assert.Equal(
            expected.Entries.Select(e => nameSelector(e.Item)).ToArray(),
            actualEntries.Select(e => nameSelector(e.Item)).ToArray());
        Assert.Equal(
            expected.Entries.Select(expectedCursor).ToArray(),
            actualEntries.Select(actualCursor).ToArray());
        Assert.Equal(
            (expected.HasNextPage, expected.HasPreviousPage),
            (await actual.HasNextPageAsync(cancellationToken), await actual.HasPreviousPageAsync(cancellationToken)));
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(expected.TotalCount, await actual.TotalCountAsync(cancellationToken));
    }

    private static async ValueTask<List<PageEntry<T>>> EntriesAsync<T>(StreamPage<T> page)
    {
        var entries = new List<PageEntry<T>>();

        await foreach (var entry in page.GetEntriesAsync())
        {
            entries.Add(entry);
        }

        return entries;
    }

    private static async Task SeedAsync(string connectionString, params (string GroupKey, int Count)[] groups)
    {
        await using var context = new SequentialItemContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        foreach (var (groupKey, count) in groups)
        {
            for (var i = 1; i <= count; i++)
            {
                context.Items.Add(new SequentialItem { GroupKey = groupKey, Name = $"{groupKey}-Item{i:D2}" });
            }
        }

        await context.SaveChangesAsync();
    }

    public class SequentialItemContext(string connectionString) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(connectionString);

        public DbSet<SequentialItem> Items => Set<SequentialItem>();
    }

    public class SequentialItem
    {
        public int Id { get; set; }

        [MaxLength(50)] public required string GroupKey { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}
#endif
