using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

/// <summary>
/// Exercises <see cref="MemoryEntryPopoverModel"/> against a <see cref="FakeMemoryStore"/>:
/// loading and rendering a curated memory's and a journal entry's header fields, scrolling,
/// the copy-id gesture, and tick-driven age recomputation.
/// </summary>
public sealed class MemoryEntryPopoverModelTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', bool shift = false) =>
        new(ch, key, shift, false, false);

    private static string RenderToText(MemoryEntryPopoverModel model, int width = 100, int height = 30)
    {
        var console = new TestConsole().Width(width);
        console.Write(model.Render(width, height));
        return console.Output;
    }

    private static MemoryEntryPopoverModel CreateModel(
        string id, MemoryCollectionFilter kind, FakeMemoryStore store, TimeProvider? timeProvider = null)
        => new(id, kind, store, timeProvider ?? new FakeTimeProvider(s_now));

    private static MemoryRecord CreateCuratedRecord(
        string id = "mem-1",
        string type = "fact",
        IReadOnlyList<string>? tags = null,
        string body = "Body text.",
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null,
        string createdBy = "bob",
        string? promotedFrom = null) => new()
        {
            Id = id,
            Type = type,
            Tags = tags ?? [],
            Body = body,
            CreatedAt = createdAt ?? s_now,
            UpdatedAt = updatedAt ?? createdAt ?? s_now,
            CreatedBy = createdBy,
            PromotedFrom = promotedFrom
        };

    private static MemoryJournalEntry CreateJournalEntry(
        string id = "jrn-1", string body = "Journal body.", DateTimeOffset? createdAt = null, string createdBy = "bob")
        => new() { Id = id, Body = body, CreatedAt = createdAt ?? s_now, CreatedBy = createdBy };

    [Fact]
    public void Render_Should_ShowTheEntryIdAsTheTitle_When_ACuratedMemoryIsLoaded()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("mem-1", text);
    }

    [Fact]
    public void Render_Should_ShowCuratedHeaderFields_When_ACuratedMemoryIsLoaded()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(
            type: "decision", tags: ["ops", "infra"], createdAt: s_now, updatedAt: s_now.AddMinutes(5));
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store, new FakeTimeProvider(s_now.AddMinutes(5)));
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Kind: curated", text);
        Assert.Contains("Type: decision", text);
        Assert.Contains("Tags: ops, infra", text);
        Assert.Contains("Created: 5m ago by bob", text);
        Assert.Contains("Updated: just now", text);
    }

    [Fact]
    public void Render_Should_ShowADashForTags_When_TheCuratedMemoryHasNoTags()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(tags: []);
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Tags: -", text);
    }

    [Fact]
    public void Render_Should_HideUpdated_When_ItMatchesCreated()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(createdAt: s_now, updatedAt: s_now);
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);
        var actual = (
            HasCreated: text.Contains("Created:", StringComparison.Ordinal),
            HasUpdated: text.Contains("Updated:", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false), actual);
    }

    [Fact]
    public void Render_Should_ShowPromotedFrom_When_TheCuratedMemoryWasPromoted()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(promotedFrom: "jrn-9");
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Promoted from: jrn-9", text);
    }

    [Fact]
    public void Render_Should_ShowJournalHeaderFieldsAndBody_When_AJournalEntryIsLoaded()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.JournalEntries["jrn-1"] = CreateJournalEntry(body: "Follow up needed.", createdAt: s_now, createdBy: "alice");
        var model = CreateModel("jrn-1", MemoryCollectionFilter.Journal, store, new FakeTimeProvider(s_now));
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("Kind: journal", text);
        Assert.Contains("Created: just now by alice", text);
        Assert.Contains("Follow up needed.", text);
    }

    [Fact]
    public void Render_Should_ShowTheFullPopoverLayout_When_ACuratedMemoryIsLoaded()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(
            type: "fact", tags: ["ops"], body: "Restart the worker on failure.", createdAt: s_now, createdBy: "bob");
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store, new FakeTimeProvider(s_now));
        model.Load(TestContext.Current.CancellationToken);
        var console = new TestConsole().Width(100);

        // act
        console.Write(model.Render(100, 30));

        // assert
        console.Output.MatchInlineSnapshot(
            """



                      ╭─mem-1────────────────────────────────────────────────────────────────────────╮
                      │                                                                              │
                      │ Kind: curated                                                                │
                      │ Type: fact                                                                   │
                      │ Tags: ops                                                                    │
                      │ Created: just now by bob                                                     │
                      │                                                                              │
                      │ Restart the worker on failure.                                               │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      ╰──────────────────────────────────────────────────────────────────────────────╯



            """);
    }

    [Fact]
    public void Render_Should_ShowTheFullPopoverLayout_When_AJournalEntryIsLoaded()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.JournalEntries["jrn-1"] = CreateJournalEntry(body: "Follow up needed.", createdAt: s_now, createdBy: "alice");
        var model = CreateModel("jrn-1", MemoryCollectionFilter.Journal, store, new FakeTimeProvider(s_now));
        model.Load(TestContext.Current.CancellationToken);
        var console = new TestConsole().Width(100);

        // act
        console.Write(model.Render(100, 30));

        // assert
        console.Output.MatchInlineSnapshot(
            """



                      ╭─jrn-1────────────────────────────────────────────────────────────────────────╮
                      │                                                                              │
                      │ Kind: journal                                                                │
                      │ Created: just now by alice                                                   │
                      │                                                                              │
                      │ Follow up needed.                                                            │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      │                                                                              │
                      ╰──────────────────────────────────────────────────────────────────────────────╯



            """);
    }

    [Fact]
    public void Render_Should_ShowTheMissingState_When_TheEntryNoLongerExists()
    {
        // arrange
        var store = new FakeMemoryStore();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var text = RenderToText(model);

        // assert
        Assert.Contains("This entry no longer exists.", text);
    }

    [Fact]
    public void HandleKey_Should_ScrollTheBody_When_JIsPressedRepeatedly()
    {
        // arrange
        var store = new FakeMemoryStore();
        var body = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"Line {i}"));
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(body: body);
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);
        var before = RenderToText(model, height: 10);

        // act
        for (var i = 0; i < 5; i++)
        {
            model.HandleKey(Key(ConsoleKey.J, 'j'));
        }

        var after = RenderToText(model, height: 10);

        // assert
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void HandleKey_Should_JumpToTheBottomAndBackToTheTop_When_ShiftGThenGArePressed()
    {
        // arrange
        var store = new FakeMemoryStore();
        var body = string.Join('\n', Enumerable.Range(0, 40).Select(i => $"Line {i}"));
        store.CuratedRecords["mem-1"] = CreateCuratedRecord(body: body);
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);
        var top = RenderToText(model, height: 10);

        // act
        model.HandleKey(Key(ConsoleKey.G, 'G', shift: true));
        var bottom = RenderToText(model, height: 10);
        model.HandleKey(Key(ConsoleKey.G, 'g'));
        var backAtTop = RenderToText(model, height: 10);

        // assert
        Assert.NotEqual(top, bottom);
        Assert.Equal(top, backAtTop);
    }

    [Fact]
    public void HandleKey_Should_ReturnCopyRequestedWithTheEntryId_When_YIsPressed()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Y, 'y'));

        // assert
        var request = Assert.IsType<PopoverResult.Request>(result);
        var copy = Assert.IsType<MemoryEntryPopoverRequest.CopyRequested>(request.Payload);
        Assert.Equal("mem-1", copy.EntryId);
    }

    [Fact]
    public void HandleKey_Should_ReturnClosed_When_EscapeIsPressed()
    {
        // arrange
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var result = model.HandleKey(Key(ConsoleKey.Escape));

        // assert
        Assert.IsType<PopoverResult.Closed>(result);
    }

    [Fact]
    public void Tick_Should_ReturnFalse_When_NoTimeHasPassed()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store, time);
        model.Load(TestContext.Current.CancellationToken);

        // act
        var dirty = model.Tick();

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Tick_Should_ReturnTrueAndUpdateTheAges_When_TimeAdvances()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeMemoryStore();
        store.CuratedRecords["mem-1"] = CreateCuratedRecord();
        var model = CreateModel("mem-1", MemoryCollectionFilter.Curated, store, time);
        model.Load(TestContext.Current.CancellationToken);

        // act
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = model.Tick();
        var text = RenderToText(model);

        // assert
        Assert.True(dirty);
        Assert.Contains("Created: 2m ago by bob", text);
    }

    [Fact]
    public void DefaultHints_Should_ListScrollCopyIdAndClose_When_Inspected()
    {
        // arrange
        // act
        var hints = MemoryEntryPopoverModel.DefaultHints;

        // assert
        Assert.Equal(
            [new("j/k", "scroll"), new("y", "copy id"), new("esc", "close")],
            hints);
    }
}
