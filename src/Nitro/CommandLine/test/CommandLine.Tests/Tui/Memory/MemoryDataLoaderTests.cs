using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tests.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

/// <summary>
/// Exercises <see cref="MemoryDataLoader"/> against a real
/// <see cref="MemoryStore"/>, matching <c>MemoryStoreTests</c>'s own
/// real-store convention.
/// </summary>
public sealed class MemoryDataLoaderTests : MemoryTestBase
{
    private readonly MemoryStore _store;
    private readonly MemoryDataLoader _loader;

    public MemoryDataLoaderTests() : base("nitro-memory-data-loader-tests")
    {
        _store = new MemoryStore(FileSystem, TimeProvider, GlobalMemoryDirectory);
        _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, CancellationToken.None).GetAwaiter().GetResult();
        _loader = new MemoryDataLoader(_store);
    }

    private Task<MemoryRecord> SaveAsync(string text, string type = "fact", IReadOnlyList<string>? tags = null)
        => _store.SaveAsync(
            new MemoryRecordCreation { Text = text, Type = type, Tags = tags ?? [], Actor = "test-agent" },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task LoadCuratedAsync_Should_ReturnRecentRecords_When_QueryIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("First.");
        await SaveAsync("Second.");

        // act
        var records = await _loader.LoadCuratedAsync("project", new MemoryQuery("", null, []), cancellationToken);

        // assert
        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task LoadCuratedAsync_Should_NarrowByType_When_QueryHasNoFreeText()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("A fact.", type: "fact");
        await SaveAsync("A decision.", type: "decision");

        // act
        var records = await _loader.LoadCuratedAsync(
            "project", new MemoryQuery("", "decision", []), cancellationToken);

        // assert
        var record = Assert.Single(records);
        Assert.Equal("decision", record.Type);
    }

    [Fact]
    public async Task LoadCuratedAsync_Should_NarrowByTag_When_QueryHasNoFreeText()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("Untagged.");
        await SaveAsync("Tagged.", tags: ["ops"]);

        // act
        var records = await _loader.LoadCuratedAsync(
            "project", new MemoryQuery("", null, ["ops"]), cancellationToken);

        // assert
        var record = Assert.Single(records);
        Assert.Contains("ops", record.Tags);
    }

    [Fact]
    public async Task LoadCuratedAsync_Should_SearchFreeText_ThroughTheStoresLexicalMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("Deploy checklist for staging.");
        await SaveAsync("Unrelated note.");

        // act
        var records = await _loader.LoadCuratedAsync(
            "project", new MemoryQuery("deploy checklist", null, []), cancellationToken);

        // assert
        var record = Assert.Single(records);
        Assert.Equal("Deploy checklist for staging.", record.Body);
    }

    [Fact]
    public async Task LoadJournalAsync_Should_ReturnRecentEntries_When_QueryIsEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _store.LogAsync(
            new MemoryJournalEntryCreation { Text = "Note one.", Actor = "test-agent" }, cancellationToken);

        // act
        var entries = await _loader.LoadJournalAsync("project", new MemoryQuery("", null, []), cancellationToken);

        // assert
        Assert.Single(entries);
    }
}
