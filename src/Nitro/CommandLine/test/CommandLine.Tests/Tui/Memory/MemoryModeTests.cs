using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tests.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

/// <summary>
/// Exercises <see cref="MemoryMode"/> and its overlays against a real
/// <see cref="MemoryStore"/>, the same way <c>MailModeRealStoreTests</c>
/// does for the mail board: memory has no fake store double, so every
/// memory TUI component test runs against the real store, matching
/// <c>MemoryStoreTests</c>'s own convention.
/// </summary>
public sealed class MemoryModeTests : MemoryTestBase
{
    private readonly MemoryStore _store;

    public MemoryModeTests() : base("nitro-memory-mode-tests")
    {
        _store = new MemoryStore(FileSystem, TimeProvider, GlobalMemoryDirectory);
        _store.EnsureProjectWorkspaceAsync(WorkspaceDirectory, CancellationToken.None).GetAwaiter().GetResult();
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

    private Task<MemoryRecord> SaveAsync(string text = "Some text.", string type = "fact", string scope = "project")
        => _store.SaveAsync(
            new MemoryRecordCreation { Text = text, Type = type, Actor = "test-agent", Scope = scope },
            TestContext.Current.CancellationToken);

    private Task<MemoryJournalEntry> LogAsync(string text = "Journal note.", string scope = "project")
        => _store.LogAsync(
            new MemoryJournalEntryCreation { Text = text, Actor = "test-agent", Scope = scope },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task OnEnter_Should_LoadCuratedMemories()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SaveAsync("First.");
        var mode = CreateMode();

        // act
        mode.OnEnter();

        // assert
        Assert.Single(mode.State.CuratedRecords);
        Assert.Equal(MemoryCollectionFilter.Curated, mode.State.Collection);
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
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveCursor_Should_TogglePaneFocus_When_Left()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();
        Assert.Equal(MemoryFocus.List, mode.State.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));

        // assert
        Assert.Equal(MemoryFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public async Task CycleView_Should_SwitchToTheJournalCollection()
    {
        // arrange
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal(MemoryCollectionFilter.Journal, mode.State.Collection);
        Assert.Single(mode.State.JournalEntries);
    }

    [Fact]
    public async Task CycleScopeRequested_Should_AdvanceThroughAllProjectGlobal()
    {
        // arrange
        await SaveAsync("Project memory.", scope: "project");
        var mode = CreateMode();
        mode.OnEnter();
        Assert.Equal("all", mode.State.Scope);

        // act
        mode.Handle(new TuiMessage.CycleScopeRequested());

        // assert
        Assert.Equal("project", mode.State.Scope);

        // act
        mode.Handle(new TuiMessage.CycleScopeRequested());

        // assert
        Assert.Equal("global", mode.State.Scope);
        Assert.Empty(mode.State.CuratedRecords);
    }

    [Fact]
    public async Task RefreshRequested_Should_ReloadMemories()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();
        Assert.Empty(mode.State.CuratedRecords);

        // act
        await SaveAsync("New memory.");
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Single(mode.State.CuratedRecords);
    }

    [Fact]
    public async Task CopySelectedId_Should_ShowInfoToast_When_ItemSelected()
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
        Assert.Equal(ToastStyle.Info, shown.Style);
    }

    [Fact]
    public void CopySelectedId_Should_ShowWarningToast_When_NoItemSelected()
    {
        // arrange
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public async Task ForgetRequested_Should_OpenConfirmation_Without_DeletingYet()
    {
        // arrange
        var saved = await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ForgetRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
        var stillThere = await _store.FindAsync(saved.Id, "project", TestContext.Current.CancellationToken);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public void ForgetRequested_Should_ShowWarnToast_When_NoCuratedMemorySelected()
    {
        // arrange: the journal collection has no notion of a selected
        // curated memory to forget.
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.CycleView(1));

        // act
        var followUp = mode.Handle(new TuiMessage.ForgetRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public async Task ForgetConfirmation_Confirmed_Should_DeleteMemory_And_RemoveItFromTheList()
    {
        // arrange
        var saved = await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.ForgetRequested());

        // act: Enter confirms from the dialog's initially focused (empty) reason field.
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
        Assert.Empty(mode.State.CuratedRecords);
        var deleted = await _store.FindAsync(saved.Id, "project", TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ForgetConfirmation_Cancelled_Should_LeaveTheMemoryUntouched()
    {
        // arrange
        var saved = await SaveAsync("First.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.ForgetRequested());

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Single(mode.State.CuratedRecords);
        var stillThere = await _store.FindAsync(saved.Id, "project", TestContext.Current.CancellationToken);
        Assert.NotNull(stillThere);
    }

    [Fact]
    public void PromoteRequested_Should_ShowWarnToast_When_NoJournalEntrySelected()
    {
        // arrange: default collection is curated, which has no notion of a
        // selected journal entry to promote.
        var mode = CreateMode();
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.PromoteRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public async Task PromoteRequested_Should_OpenForm_When_JournalEntrySelected()
    {
        // arrange
        await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.CycleView(1));

        // act
        var followUp = mode.Handle(new TuiMessage.PromoteRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public async Task PromoteForm_Submit_Should_PromoteJournalEntry_And_ShowSuccessToast()
    {
        // arrange
        var entry = await LogAsync("Note one.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.PromoteRequested());
        Type(mode, "decision");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);

        var curatedId = MemoryPromotedId.Derive(entry.Scope, entry.Id);
        var promoted = await _store.FindAsync(curatedId, "project", TestContext.Current.CancellationToken);
        Assert.NotNull(promoted);
        Assert.Equal("decision", promoted!.Type);
    }

    [Fact]
    public async Task PromoteForm_Submit_Should_ReportAlreadyPromoted_When_TheJournalEntryWasPromotedBefore()
    {
        // arrange: the entry was already promoted outside the tab (for
        // example via the CLI); promoting it again from the tab must be
        // idempotent, not an error.
        var entry = await LogAsync("Note one.");
        await _store.PromoteAsync(entry.Id, entry.Scope, "fact", [], TestContext.Current.CancellationToken);

        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.PromoteRequested());
        Type(mode, "decision");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Success, shown.Style);
        Assert.Contains("already promoted", shown.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PromoteForm_Cancel_Should_CloseImmediately_When_NotDirty()
    {
        // arrange
        await LogAsync("Note.");
        var mode = CreateMode();
        mode.OnEnter();
        mode.Handle(new TuiMessage.CycleView(1));
        mode.Handle(new TuiMessage.PromoteRequested());

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public async Task SearchRequested_Should_OpenSearchForm()
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
    public async Task SearchForm_Apply_Should_NarrowTheListByTag()
    {
        // arrange
        await SaveAsync("Deploy checklist.");
        await _store.SaveAsync(
            new MemoryRecordCreation { Text = "Ops note.", Type = "fact", Tags = ["ops"], Actor = "test-agent" },
            TestContext.Current.CancellationToken);
        var mode = CreateMode();
        mode.OnEnter();
        Assert.Equal(2, mode.State.CuratedRecords.Count);
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "tag:ops");

        // act
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        Assert.False(mode.IsInputCapturing);
        var record = Assert.Single(mode.State.CuratedRecords);
        Assert.Contains("ops", record.Tags);
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
