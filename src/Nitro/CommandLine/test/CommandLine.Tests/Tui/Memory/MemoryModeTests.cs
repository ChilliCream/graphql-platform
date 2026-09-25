using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

/// <summary>
/// Exercises <see cref="MemoryMode"/> against a real <see cref="MemoryStore"/>.
/// </summary>
public sealed class MemoryModeTests : MemoryTestBase
{
    private readonly MemoryStore _store;

    public MemoryModeTests() : base("nitro-memory-mode-tests")
    {
        _store = new MemoryStore(FileSystem, TimeProvider, new AgentDatabase());
        InitializeWorkspace();
    }

    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static void Type(MemoryMode mode, string text)
    {
        foreach (var c in text)
        {
            mode.HandleRawKey(Key(c));
        }
    }

    private MemoryMode CreateMode() => new(_store, TimeProvider);

    private static string RenderToText(MemoryMode mode, int width = 100, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    private Task<MemoryRecord> SaveAsync(
        string text = "Some text.", string type = "fact", IReadOnlyList<string>? tags = null)
        => _store.SaveAsync(
            new MemoryRecordCreation { Text = text, Type = type, Tags = tags ?? [], Actor = "test-agent" },
            TestContext.Current.CancellationToken);

    private Task<MemoryJournalEntry> LogAsync(string text = "Journal note.")
        => _store.LogAsync(
            new MemoryJournalEntryCreation { Text = text, Actor = "test-agent" },
            TestContext.Current.CancellationToken);

    [Fact]
    public void Render_Should_ShowTheEmptyStateMessage_When_NoMemoryExists()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("No memory yet.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_Should_ShowTheLoadError_When_TheStoreRejectsTheRead()
    {
        // arrange
        // a store over a directory that has no agent workspace
        var noWorkspaceDirectory = Path.Combine(Path.GetDirectoryName(WorkingDirectory)!, "no-workspace");
        Directory.CreateDirectory(noWorkspaceDirectory);
        var storeWithNoWorkspace = new MemoryStore(new TestFileSystem(noWorkspaceDirectory), TimeProvider, new AgentDatabase());
        var mode = new MemoryMode(storeWithNoWorkspace, TimeProvider);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("No agent workspace found", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_Should_ShowTheRowCount_When_HeaderIsRendered()
    {
        // arrange
        await SaveAsync("First.");
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Memory (2)", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_Should_OrderRowsByTimeDescending_When_CuratedAndJournalRowsExist()
    {
        // arrange
        await SaveAsync("Oldest curated.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        await LogAsync("Middle journal.");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        await SaveAsync("Newest curated.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        var newestIndex = text.IndexOf("Newest curated.", StringComparison.Ordinal);
        var middleIndex = text.IndexOf("Middle journal.", StringComparison.Ordinal);
        var oldestIndex = text.IndexOf("Oldest curated.", StringComparison.Ordinal);
        Assert.True(newestIndex >= 0 && middleIndex > newestIndex && oldestIndex > middleIndex);
    }

    [Fact]
    public async Task Render_Should_ShowHeaderRuleAndRows_When_TwoCuratedAndOneJournalRowsArePresent()
    {
        // arrange
        await SaveAsync("Ops runbook.", type: "fact", tags: ["ops"]);
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        await SaveAsync("Deploy notes.", type: "decision");
        TimeProvider.Advance(TimeSpan.FromMinutes(1));
        await LogAsync("Follow up needed.");
        var mode = CreateMode();
        mode.OnEnter();
        var console = new TestConsole().Width(100);

        // act
        console.Write(mode.Render(100, 9));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Memory (3)───────────────────────────────────────────────────────────────────────────────────────╮
            │                                                                                                  │
            │    KIND        TYPE          TAGS            AGE           BODY                                  │
            │ ──────────────────────────────────────────────────────────────────────────────────────────────── │
            │                                                                                                  │
            │ >  journal     -             -               just now      Follow up needed.                     │
            │    curated     decision      -               1m ago        Deploy notes.                         │
            │    curated     fact          ops             2m ago        Ops runbook.                          │
            ╰──────────────────────────────────────────────────────────────────────────────────────────────────╯

            """);
    }

    [Fact]
    public async Task MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        await SaveAsync("First.");
        await SaveAsync("Second.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.SelectedRow);
    }

    [Fact]
    public async Task MoveSelectionToEdge_Should_SelectLastRow_When_Bottom()
    {
        // arrange
        await SaveAsync("First.");
        await SaveAsync("Second.");
        await SaveAsync("Third.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public async Task OpenSelected_Should_BeANoOp_When_ARowIsSelected()
    {
        // arrange
        await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(followUp);
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public async Task CopySelectedId_Should_ReturnTheRowId_When_ARowIsSelected()
    {
        // arrange
        var saved = await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(saved.Id, shown.Text);
    }

    [Fact]
    public void CopySelectedId_Should_ReturnAWarning_When_NoRowIsSelected()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
    }

    [Fact]
    public async Task RefreshRequested_Should_ReloadRowsFromTheStore_When_ANewMemoryWasAdded()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();
        Assert.Empty(mode.State.Rows);
        await SaveAsync("New memory.");

        // act
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Single(mode.State.Rows);
    }

    [Fact]
    public async Task CycleView_Should_NarrowToCuratedOnly_When_PressedOnceFromAll()
    {
        // arrange
        await SaveAsync("First.");
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal(MemoryCollectionFilter.Curated, mode.State.Filter);
        var row = Assert.Single(mode.State.Rows);
        Assert.Equal(MemoryCollectionFilter.Curated, row.Kind);
    }

    [Fact]
    public async Task CycleView_Should_NarrowToJournalOnly_When_PressedTwiceFromAll()
    {
        // arrange
        await SaveAsync("First.");
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal(MemoryCollectionFilter.Journal, mode.State.Filter);
        var row = Assert.Single(mode.State.Rows);
        Assert.Equal(MemoryCollectionFilter.Journal, row.Kind);
    }

    [Fact]
    public async Task CycleView_Should_ReturnToAll_When_PressedThreeTimes()
    {
        // arrange
        await SaveAsync("First.");
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal(MemoryCollectionFilter.All, mode.State.Filter);
        Assert.Equal(2, mode.State.Rows.Count);
    }

    [Fact]
    public async Task SearchRequested_Should_OpenTheSearchForm_When_Requested()
    {
        // arrange
        await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.SearchRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public async Task SearchForm_Apply_Should_NarrowRowsByTag_When_TagTermIsUsed()
    {
        // arrange
        await SaveAsync("Deploy checklist.");
        await SaveAsync("Ops note.", tags: ["ops"]);
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "tag:ops");

        // act
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        Assert.False(mode.IsInputCapturing);
        var row = Assert.Single(mode.State.Rows);
        Assert.Equal("Ops note.", row.Body);
    }

    [Fact]
    public async Task SearchForm_Apply_Should_NarrowCuratedAndJournalRows_When_FreeTextIsUsed()
    {
        // arrange
        await SaveAsync("Deploy checklist for staging.");
        await LogAsync("Deploy failed overnight.");
        await LogAsync("Unrelated note.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "deploy");

        // act
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        Assert.False(mode.IsInputCapturing);
        Assert.Equal(2, mode.State.Rows.Count);
    }

    [Fact]
    public async Task SearchForm_Cancel_Should_LeaveRowsUnfiltered_When_FormIsCancelled()
    {
        // arrange
        await SaveAsync("First.");
        await SaveAsync("Second.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "no-match");

        // act
        mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.False(mode.IsInputCapturing);
        Assert.Equal(2, mode.State.Rows.Count);
    }

    [Fact]
    public async Task Render_Should_ShowFilteredSuffix_When_SearchTextIsSet()
    {
        // arrange
        await SaveAsync("Deploy checklist.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "deploy");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Memory (1) (filtered)", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_Should_DropTagsColumn_When_WidthIsTooNarrowForEveryColumn()
    {
        // arrange
        await SaveAsync("Deploy checklist.", tags: ["ops"]);
        var mode = CreateMode();
        mode.OnEnter();
        var wide = RenderToText(mode, width: 100);

        // act
        var narrow = RenderToText(mode, width: 55);
        var actual = (
            WideHasTags: wide.Contains("ops", StringComparison.Ordinal),
            NarrowHasTags: narrow.Contains("ops", StringComparison.Ordinal),
            NarrowHasType: narrow.Contains("fact", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false, true), actual);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var exception = Record.Exception(() => mode.Render(0, 0));

        // assert
        Assert.Null(exception);
    }
}
