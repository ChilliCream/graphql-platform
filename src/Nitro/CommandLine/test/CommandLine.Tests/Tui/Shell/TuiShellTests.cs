using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class TuiShellTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key, ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        new(
            keyChar,
            key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    private static TuiShell CreateShell(FakeTuiMode mode, int width = 80, int height = 24) =>
        new(new KeyDispatcher(KeyMap.CreateDefaultGlobal()), mode, width, height);

    private static TuiShell CreateShellWithModes(
        ITuiMode initialMode,
        FakeTaskStore store,
        out SearchMode searchMode,
        out DependencyTreeView treeView)
    {
        searchMode = new SearchMode(store);
        treeView = new DependencyTreeView(store, rootId: "");

        return new TuiShell(
            new KeyDispatcher(KeyMap.CreateDefaultGlobal()),
            initialMode,
            80,
            24,
            searchMode,
            treeView,
            store,
            actor: "tester");
    }

    private static string RenderToText(TuiShell shell)
    {
        var console = new TestConsole().Width(80);
        console.Write(shell.Render());
        return console.Output;
    }

    private static string RenderToText(TuiShell shell, int width)
    {
        var console = new TestConsole().Width(width);
        console.Write(shell.Render());
        return console.Output;
    }

    [Fact]
    public void Constructor_Should_CallOnEnter_OnActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();

        // act
        CreateShell(mode);

        // assert
        Assert.True(mode.EnterCalled);
    }

    [Fact]
    public void Handle_Should_OpenConfirmDialog_When_QuitRequested()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // assert
        Assert.True(dirty);
        Assert.Contains("Quit?", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_RaiseQuitConfirmed_When_YPressedWhileConfirmActive()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('y', ConsoleKey.Y)));

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
    }

    [Fact]
    public void Handle_Should_CloseConfirmDialogWithoutQuitting_When_NPressedWhileConfirmActive()
    {
        // arrange
        var mode = new FakeTuiMode { RenderText = "board" };
        var shell = CreateShell(mode);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('n', ConsoleKey.N)));

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        var text = RenderToText(shell);
        Assert.DoesNotContain("Quit?", text);
        Assert.Contains("board", text);
    }

    [Fact]
    public void Handle_Should_PrioritizeConfirmDialogKeys_OverGlobalCopyBinding_When_ConfirmActive()
    {
        // The global table binds 'y' to CopySelectedId; while the confirm dialog is
        // active it must win that binding instead of falling through to global.
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('y', ConsoleKey.Y)));

        // assert
        Assert.True(confirmed);
        Assert.DoesNotContain(mode.HandledMessages, m => m is TuiMessage.CopySelectedId);
    }

    [Fact]
    public void Handle_Should_ForwardRefreshRequested_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Handle_Should_ForwardDataChangedEvent_ToActiveModeAsRefreshRequested()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        var dirty = shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.True(dirty);
        Assert.Contains(mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Handle_Should_DelegateUnhandledMessage_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));

        // assert
        var moveCursor = Assert.IsType<TuiMessage.MoveCursor>(Assert.Single(mode.HandledMessages));
        Assert.Equal(CursorDirection.Down, moveCursor.Direction);
    }

    [Fact]
    public void Handle_Should_DispatchFollowUpMessages_FromActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains("saved", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ExpireToast_When_TickIsFarEnoughInTheFuture()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));
        Assert.Contains("saved", RenderToText(shell));

        // act
        var dirty = shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow.AddSeconds(4)));

        // assert
        Assert.True(dirty);
        Assert.DoesNotContain("saved", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ReturnFalse_When_TickWithNoActiveToast()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());

        // act
        var dirty = shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow));

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Handle_Should_ResizeActiveMode_ReservingStatusRow_When_ResizeEvent()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode, width: 80, height: 24);

        // act
        var dirty = shell.Handle(new TuiEvent.ResizeEvent(100, 30));

        // assert
        Assert.True(dirty);
        Assert.Equal((100, 29), Assert.Single(mode.ResizeCalls));
    }

    [Fact]
    public void Render_Should_PassContentHeight_ReservingStatusRow_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode, width: 80, height: 24);

        // act
        shell.Render();

        // assert
        Assert.Equal((80, 23), Assert.Single(mode.RenderCalls));
    }

    [Fact]
    public void Handle_Should_SwitchToSearchAndFocusInput_When_SlashPressed()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out var search, out _);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));

        // assert
        Assert.True(dirty);
        Assert.Equal(SearchFocus.Input, search.Focus);
        Assert.Contains("Results", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_PopBackToPreviousMode_When_EscapePressedAfterSwitchingModes()
    {
        // arrange
        var store = new FakeTaskStore();
        var initialMode = new FakeTuiMode { RenderText = "board" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));
        Assert.Contains("Results", RenderToText(shell));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.True(dirty);
        Assert.Contains("board", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ShowToast_When_TreeRequestedWithNoSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        var initialMode = new FakeTuiMode();
        var shell = CreateShellWithModes(initialMode, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('t', ConsoleKey.T)));

        // assert
        Assert.Contains("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_SwitchToTreeRootedOnSelection_When_TreeRequested()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out var tree);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('t', ConsoleKey.T)));

        // assert
        Assert.True(dirty);
        Assert.Equal("a", tree.RootId);
    }

    [Fact]
    public void Handle_Should_ShowToast_When_EnterPressedOnBoardWithNoSelection()
    {
        // arrange: an empty board has no focused-column selection.
        var store = new FakeTaskStore();
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = CreateShellWithModes(board, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Contains("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenTaskDetail_When_EnterPressedOnBoardSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = CreateShellWithModes(board, store, out _, out _);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        var rendered = RenderToText(shell);
        Assert.True(dirty);
        Assert.Contains("Board task", rendered);
        Assert.DoesNotContain("Detail view not available yet.", rendered);
    }

    [Fact]
    public void Handle_Should_ReturnToBoardWithSelectionPreserved_When_EscapePressedAfterOpeningDetailFromBoard()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = CreateShellWithModes(board, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));
        Assert.Contains("Board task", RenderToText(shell));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.True(dirty);
        Assert.Equal("a-1", board.State.Columns[0].SelectedTaskId);
        Assert.DoesNotContain("Detail view not available yet.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ShowToast_When_EditRequestedWithNoSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // assert
        Assert.Contains("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenEditorAndWriteChangedTitle_When_EditRequestedThenSaved()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", "Old title");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        Assert.Contains("Edit Task", RenderToText(shell));

        // act: append a character to the focused title field, then tab to the
        // button row (7 fields: title/status/priority/type/labels/description/
        // notes) and activate the default-selected Save button.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 7; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.UpdatedId);
        Assert.Equal("Old title!", store.UpdateReceived!.Title);
        Assert.Equal("tester", store.Actor);
        Assert.Contains("Updated task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenEditorFromBoardSelection_When_BoardModeIsActive()
    {
        // arrange: a real BoardMode (not FakeTuiMode) supplies SelectedTaskId
        // from its focused column's selected row.
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = CreateShellWithModes(board, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // assert
        var rendered = RenderToText(shell);
        Assert.Contains("Edit Task", rendered);
        Assert.DoesNotContain("No task selected.", rendered);
    }

    [Fact]
    public void Handle_Should_CreateChildTaskFromBoardSelection_When_BoardModeIsActive()
    {
        // arrange: create-as-child reads the active mode's SelectedTaskId as
        // the new task's parent id.
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = CreateShellWithModes(board, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));

        // act: fill the required title field, tab to the button row, submit.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 5; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a-1", store.CreationReceived!.ParentId);
    }

    [Fact]
    public void Handle_Should_CloseEditorWithoutWriting_When_EscapePressedWhileEditing()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.Null(store.UpdatedId);
        Assert.DoesNotContain("Edit Task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenDiscardConfirmation_When_EscapePressedWhileEditingDirtyEditorForm()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", "Title");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert: the editor stays open behind the confirmation, unwritten.
        Assert.Null(store.UpdatedId);
        Assert.Contains("Discard unsaved changes?", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ReturnToEditorFormWithValuesIntact_When_DiscardCancelled()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", "Title");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // act: Escape on the discard confirmation cancels it, not the editor.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        var text = RenderToText(shell);
        Assert.DoesNotContain("Discard unsaved changes?", text);
        Assert.Contains("Edit Task", text);
    }

    [Fact]
    public void Handle_Should_CloseEditorWithoutWriting_When_DiscardConfirmed()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", "Title");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // act: Enter on the discard confirmation's focused reason field confirms it.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Null(store.UpdatedId);
        var text = RenderToText(shell);
        Assert.DoesNotContain("Discard unsaved changes?", text);
        Assert.DoesNotContain("Edit Task", text);
    }

    [Fact]
    public void Handle_Should_OpenDiscardConfirmation_When_EscapePressedWhileCreatingDirtyCreateForm()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert: the create form stays open behind the confirmation.
        Assert.Null(store.CreationReceived);
        Assert.Contains("Discard unsaved changes?", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_CloseCreateFormWithoutCreating_When_DiscardConfirmed()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Null(store.CreationReceived);
        var text = RenderToText(shell);
        Assert.DoesNotContain("Discard unsaved changes?", text);
        Assert.DoesNotContain("Create Task", text);
    }

    [Fact]
    public void Handle_Should_CloseTask_When_CloseOrReopenRequestedOnOpenTask()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Open);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('x', ConsoleKey.X)));
        Assert.Contains("Close task", RenderToText(shell));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal(["a"], store.ClosedIds);
        Assert.Equal("tester", store.Actor);
        Assert.Contains("Closed task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ReopenTask_When_CloseOrReopenRequestedOnClosedTask()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Closed);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('x', ConsoleKey.X)));
        Assert.Contains("Reopen task", RenderToText(shell));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.ReopenedId);
        Assert.Contains("Reopened task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_DeleteTask_When_DeleteRequested()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('X', ConsoleKey.X, ConsoleModifiers.Shift)));
        Assert.Contains("Delete task", RenderToText(shell));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.DeletedId);
        Assert.Contains("Deleted task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ShowToast_When_StatusPickerRequestedWithNoSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S)));

        // assert
        Assert.Contains("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenStatusPickerAndWriteSelectedStatus_When_StatusPickerRequestedThenApplied()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Open);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S)));
        Assert.Contains("Status", RenderToText(shell));

        // act: move down once to In Progress, then apply.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.UpdatedId);
        Assert.True(store.UpdateReceived!.StatusGiven);
        Assert.Equal(TaskStates.InProgress, store.UpdateReceived.Status);
        Assert.Equal("tester", store.Actor);
        Assert.Contains("Status set to", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_CancelStatusPickerWithoutWriting_When_EscapePressedWhilePicking()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Open);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.Null(store.UpdatedId);
        Assert.DoesNotContain("Status", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenCloseConfirmation_When_ClosedPickedOnStatusPicker()
    {
        // arrange: picking Closed on the status picker is not a bare status
        // write, it routes through the same close confirmation flow as x.
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Open);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S)));

        // act: move down to Closed (Open, In Progress, Blocked, Deferred, Closed).
        for (var i = 0; i < 4; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Null(store.UpdatedId);
        Assert.Contains("Close task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ShowToast_When_PriorityPickerRequestedWithNoSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('p', ConsoleKey.P)));

        // assert
        Assert.Contains("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenPriorityPickerAndWriteSelectedPriority_When_PriorityPickerRequestedThenApplied()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('p', ConsoleKey.P)));
        Assert.Contains("Priority", RenderToText(shell));

        // act: move up once to P1, then apply.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('k', ConsoleKey.K)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.UpdatedId);
        Assert.True(store.UpdateReceived!.PriorityGiven);
        Assert.Equal(1, store.UpdateReceived.Priority);
        Assert.Contains("Priority set to", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenCreateFormAndRouteSubsequentKeys_ToForm_When_CreateTaskRequested()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = new FakeTuiMode();
        var shell = CreateShellWithModes(mode, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));
        Assert.Contains("Create Task", RenderToText(shell));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));

        // assert: the 'j' that would otherwise move the active mode's cursor
        // was absorbed by the modal create form instead.
        Assert.Empty(mode.HandledMessages);
    }

    [Fact]
    public void Handle_Should_PresetEpicType_When_CreateEpicRequestedThenSubmitted()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('C', ConsoleKey.C, ConsoleModifiers.Shift)));
        Assert.Contains("Create Epic", RenderToText(shell));

        // act: type a title, tab to the button row (5 fields: title/type/
        // priority/labels/description), then activate the default Create button.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 5; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal(TaskTypes.Epic, store.CreationReceived!.Type);
    }

    [Fact]
    public void Handle_Should_PassSelectedTaskIdAsParentId_When_CreateFormSubmittedWithSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 5; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Equal("a", store.CreationReceived!.ParentId);
    }

    [Fact]
    public void Handle_Should_CreateTopLevelTaskWithNoToast_When_CreateFormSubmittedWithNoSelection()
    {
        // arrange: creating never requires a selection, so a null
        // SelectedTaskId must not trigger the "No task selected." toast that
        // gates edit, lifecycle, and the pickers.
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 5; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Null(store.CreationReceived!.ParentId);
        Assert.DoesNotContain("No task selected.", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ToastRefreshModeAndSelectCreatedTask_When_CreateFormSubmittedSuccessfully()
    {
        // arrange
        var store = new FakeTaskStore { CreationResult = new TaskCreationResult { Id = "a2" } };
        var mode = new FakeTuiMode();
        var shell = CreateShellWithModes(mode, store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('!', ConsoleKey.NoName)));

        for (var i = 0; i < 5; i++)
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        }

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Contains("Created task 'a2'.", RenderToText(shell));
        Assert.Contains(mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
        Assert.Equal(["a2"], mode.SelectTaskCalls);
    }

    [Fact]
    public void Handle_Should_CloseCreateFormWithoutCreating_When_EscapePressedWhileCreating()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out _, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('c', ConsoleKey.C)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.Null(store.CreationReceived);
        Assert.DoesNotContain("Create Task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_RouteRawKeyToSearchQueryInput_When_SearchModeHasInputFocus()
    {
        // arrange
        var store = new FakeTaskStore();
        var shell = CreateShellWithModes(new FakeTuiMode(), store, out var search, out _);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));

        // act: 'j' would move a list cursor globally, but with the query
        // input focused it must be typed instead.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));

        // assert
        Assert.Equal("j", search.QueryText);
    }

    [Fact]
    public void Render_Should_ShowGlobalFooterHints_When_NoOverlayOrToastIsActive()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());

        // act
        var text = RenderToText(shell);

        // assert: the curated global hint set fits an 80-column footer
        // untruncated.
        Assert.Contains("move", text);
        Assert.Contains("open", text);
        Assert.Contains("refresh", text);
        Assert.Contains("copy id", text);
        Assert.Contains("zoom", text);
        Assert.Contains("edit", text);
        Assert.Contains("back", text);
        Assert.Contains("quit", text);
        Assert.DoesNotContain("…", text);
    }

    [Fact]
    public void Render_Should_ReplaceFooterWithToast_When_ToastIsActive()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));
        var text = RenderToText(shell);

        // assert: the toast is showing, so the footer's own hints are not.
        Assert.Contains("saved", text);
        Assert.DoesNotContain("quit", text);
    }

    [Fact]
    public void Render_Should_ShowFooterAgain_When_ToastExpires()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // act
        shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow.AddSeconds(4)));
        var text = RenderToText(shell);

        // assert
        Assert.DoesNotContain("saved", text);
        Assert.Contains("quit", text);
    }

    [Fact]
    public void Render_Should_ShowQuitDialogHints_PlusGlobalHints_When_QuitConfirmIsActive()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        var text = RenderToText(shell);

        // assert: the quit dialog's own hints, plus the global table it
        // still falls back to for anything it does not bind itself.
        Assert.Contains("confirm", text);
        Assert.Contains("cancel", text);
        Assert.Contains("move", text);
    }

    [Fact]
    public void Render_Should_ShowOnlyFormHints_NoGlobalHints_When_TaskEditorFormIsActive()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        var text = RenderToText(shell);

        // assert: the editor form swallows every key itself, so the global
        // hints (which would not actually work) are not shown alongside it.
        Assert.Contains("next field", text);
        Assert.Contains("save", text);
        Assert.Contains("cancel", text);
        Assert.DoesNotContain("quit", text);
    }

    [Fact]
    public void Render_Should_ShowOnlyPickerHints_NoGlobalHints_When_QuickPickerIsActive()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a", status: TaskStates.Open);
        var initialMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = CreateShellWithModes(initialMode, store, out _, out _);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S)));
        var text = RenderToText(shell);

        // assert
        Assert.Contains("select", text);
        Assert.Contains("apply", text);
        Assert.Contains("cancel", text);
        Assert.DoesNotContain("quit", text);
    }

    [Fact]
    public void Render_Should_TruncateFooterWithEllipsis_When_WidthCannotFitEveryHint()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode(), width: 15);

        // act
        var text = RenderToText(shell, width: 15);

        // assert: the first (and narrowest-fitting) hint survives, later
        // ones are dropped behind a trailing ellipsis.
        Assert.Contains("move", text);
        Assert.Contains("…", text);
        Assert.DoesNotContain("quit", text);
    }
}
