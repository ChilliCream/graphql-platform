using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Exercises <see cref="MemoryStore"/> against the workspace database that
/// holds memory alongside tasks and mail: the curated vertical (save,
/// update, forget, find, recent, search) and the journal vertical (log,
/// promote, unpromoted).
/// </summary>
public sealed class MemoryStoreTests : MemoryTestBase
{
    private readonly MemoryStore _store;

    public MemoryStoreTests() : base("nitro-memory-store-tests")
    {
        InitializeWorkspace();
        _store = new MemoryStore(FileSystem, TimeProvider, new AgentDatabase());
    }

    private Task<MemoryRecord> SaveAsync(
        string text = "Some text.", string type = "fact", IReadOnlyList<string>? tags = null)
        => _store.SaveAsync(
            new MemoryRecordCreation
            {
                Text = text,
                Type = type,
                Tags = tags ?? [],
                Actor = "test-agent"
            },
            TestContext.Current.CancellationToken);

    private Task<MemoryJournalEntry> LogAsync(string text = "Journal note.")
        => _store.LogAsync(
            new MemoryJournalEntryCreation { Text = text, Actor = "test-agent" },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task SaveAsync_Should_PersistEveryFieldAndItsTags()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var saved = await SaveAsync("Prefer pnpm.", "preference", ["build", "tooling"]);
        var found = await _store.FindAsync(saved.Id, cancellationToken);

        // assert
        Assert.NotNull(found);
        Assert.Equal("Prefer pnpm.", found.Body);
        Assert.Equal("preference", found.Type);
        Assert.Equal(["build", "tooling"], found.Tags);
        Assert.Equal("test-agent", found.CreatedBy);
    }

    [Fact]
    public async Task SaveAsync_Should_Throw_When_NoWorkspaceExists()
    {
        // arrange: a store rooted outside any agent workspace.
        var outside = Directory.CreateTempSubdirectory("nitro-memory-no-workspace");

        try
        {
            var store = new MemoryStore(
                new TestFileSystem(outside.FullName), TimeProvider, new AgentDatabase());

            // act & assert
            await Assert.ThrowsAsync<ExitException>(
                () => store.SaveAsync(
                    new MemoryRecordCreation { Text = "x", Type = "fact", Actor = "test-agent" },
                    TestContext.Current.CancellationToken));
        }
        finally
        {
            outside.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FindAsync_Should_ReturnNull_When_TheMemoryDoesNotExist()
    {
        // arrange & act
        var found = await _store.FindAsync(
            "01m0x9svd4a9h8835319mamxpg", TestContext.Current.CancellationToken);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task GetRecentCuratedAsync_Should_OrderByUpdatedAtDescending()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = await SaveAsync("First.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var second = await SaveAsync("Second.");

        // act
        var recent = await _store.GetRecentCuratedAsync(limit: null, cancellationToken);

        // assert
        Assert.Equal([second.Id, first.Id], recent.Select(record => record.Id));
    }

    [Fact]
    public async Task GetRecentCuratedAsync_Should_RespectTheLimit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("First.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var second = await SaveAsync("Second.");

        // act
        var recent = await _store.GetRecentCuratedAsync(limit: 1, cancellationToken);

        // assert
        Assert.Equal([second.Id], recent.Select(record => record.Id));
    }

    [Fact]
    public async Task UpdateAsync_Should_ReplaceTextAndTypeAndMoveUpdatedAt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var saved = await SaveAsync("Before.");
        TimeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var updated = await _store.UpdateAsync(
            saved.Id,
            new MemoryRecordUpdate
            {
                Text = "After.",
                TextGiven = true,
                Type = "decision",
                TypeGiven = true
            },
            cancellationToken);

        // assert
        Assert.Equal("After.", updated.Body);
        Assert.Equal("decision", updated.Type);
        Assert.True(updated.UpdatedAt > updated.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_Should_AddAndRemoveTags()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var saved = await SaveAsync(tags: ["keep", "drop"]);

        // act
        var updated = await _store.UpdateAsync(
            saved.Id,
            new MemoryRecordUpdate { AddTags = ["added"], RemoveTags = ["drop"] },
            cancellationToken);

        // assert
        Assert.Equal(["added", "keep"], updated.Tags);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_TheMemoryDoesNotExist()
    {
        // arrange & act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.UpdateAsync(
                "01m0x9svd4a9h8835319mamxpg",
                new MemoryRecordUpdate { Text = "x", TextGiven = true },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ForgetAsync_Should_DeleteTheMemoryAndItsTags()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var saved = await SaveAsync(tags: ["gone"]);

        // act
        var forgotten = await _store.ForgetAsync(saved.Id, cancellationToken);

        // assert: the record is returned as it was, and no longer readable.
        Assert.Equal(saved.Id, forgotten.Id);
        Assert.Null(await _store.FindAsync(saved.Id, cancellationToken));
    }

    [Fact]
    public async Task SearchCuratedAsync_Should_MatchBodyText()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var wanted = await SaveAsync("The parser rejects trailing commas.");
        await SaveAsync("Unrelated note about deploys.");

        // act
        var results = await _store.SearchCuratedAsync(
            "parser", [], type: null, since: null, limit: null, cancellationToken);

        // assert
        Assert.Equal([wanted.Id], results.Select(record => record.Id));
    }

    [Fact]
    public async Task SearchCuratedAsync_Should_NarrowByTypeAndTag()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var wanted = await SaveAsync("Deploy on Fridays.", "decision", ["ops"]);
        await SaveAsync("Deploy on Fridays.", "fact", ["ops"]);
        await SaveAsync("Deploy on Fridays.", "decision", ["other"]);

        // act
        var results = await _store.SearchCuratedAsync(
            "deploy", ["ops"], "decision", since: null, limit: null, cancellationToken);

        // assert
        Assert.Equal([wanted.Id], results.Select(record => record.Id));
    }

    [Fact]
    public async Task SearchCuratedAsync_Should_TreatTheQueryAsLiteralText()
    {
        // arrange: FTS5 operator syntax must be matched as text, not run.
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("Plain note.");

        // act
        var results = await _store.SearchCuratedAsync(
            "note OR anything", [], type: null, since: null, limit: null, cancellationToken);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchCuratedAsync_Should_SeeAnUpdatedBody()
    {
        // arrange: the schema's triggers keep the index in step, so there is
        // no rebuild step between the write and the search that finds it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var saved = await SaveAsync("Original wording.");

        await _store.UpdateAsync(
            saved.Id,
            new MemoryRecordUpdate { Text = "Replacement wording.", TextGiven = true },
            cancellationToken);

        // act
        var stale = await _store.SearchCuratedAsync(
            "original", [], type: null, since: null, limit: null, cancellationToken);
        var fresh = await _store.SearchCuratedAsync(
            "replacement", [], type: null, since: null, limit: null, cancellationToken);

        // assert
        Assert.Empty(stale);
        Assert.Equal([saved.Id], fresh.Select(record => record.Id));
    }

    [Fact]
    public async Task LogAsync_Should_PersistTheEntry()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var entry = await LogAsync("Investigated the flaky test.");
        var found = await _store.FindJournalEntryAsync(entry.Id, cancellationToken);

        // assert
        Assert.NotNull(found);
        Assert.Equal("Investigated the flaky test.", found.Body);
        Assert.Equal("test-agent", found.CreatedBy);
    }

    [Fact]
    public async Task SearchJournalAsync_Should_MatchEveryWordAsASubstring()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var wanted = await LogAsync("Investigated the flaky watcher test.");
        await LogAsync("Investigated the deploy pipeline.");

        // act
        var results = await _store.SearchJournalAsync(
            "flaky investigated", since: null, limit: null, cancellationToken);

        // assert
        Assert.Equal([wanted.Id], results.Select(entry => entry.Id));
    }

    [Fact]
    public async Task PromoteAsync_Should_CopyTheEntryIntoACuratedMemory()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = await LogAsync("Investigated the flaky test.");

        // act
        var outcome = await _store.PromoteAsync(entry.Id, "decision", ["flaky"], cancellationToken);

        // assert
        Assert.False(outcome.AlreadyPromoted);
        Assert.Equal("Investigated the flaky test.", outcome.Record.Body);
        Assert.Equal("decision", outcome.Record.Type);
        Assert.Equal(["flaky"], outcome.Record.Tags);
        Assert.Equal(entry.Id, outcome.Record.PromotedFrom);
    }

    [Fact]
    public async Task PromoteAsync_Should_ReturnTheFirstOutcome_When_PromotedTwice()
    {
        // arrange: the unique promoted_from index makes the second promote
        // affect no rows, so it reports what the first one produced rather
        // than duplicating or failing.
        var cancellationToken = TestContext.Current.CancellationToken;
        var entry = await LogAsync("Investigated the flaky test.");
        var first = await _store.PromoteAsync(entry.Id, "decision", ["flaky"], cancellationToken);

        // act
        var second = await _store.PromoteAsync(entry.Id, "fact", [], cancellationToken);

        // assert: the winner's type and tags, not this call's own attempt.
        Assert.True(second.AlreadyPromoted);
        Assert.Equal(first.Record.Id, second.Record.Id);
        Assert.Equal("decision", second.Record.Type);
        Assert.Equal(["flaky"], second.Record.Tags);
    }

    [Fact]
    public async Task PromoteAsync_Should_Throw_When_TheJournalEntryDoesNotExist()
    {
        // arrange & act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.PromoteAsync(
                "01m0x9svd4a9h8835319mamxpg", "fact", [], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetUnpromotedJournalEntriesAsync_Should_ExcludePromotedEntries()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var promoted = await LogAsync("Already curated.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        var pending = await LogAsync("Still raw.");
        await _store.PromoteAsync(promoted.Id, "fact", [], cancellationToken);

        // act
        var unpromoted = await _store.GetUnpromotedJournalEntriesAsync(cancellationToken);

        // assert
        Assert.Equal([pending.Id], unpromoted.Select(entry => entry.Id));
    }
}
