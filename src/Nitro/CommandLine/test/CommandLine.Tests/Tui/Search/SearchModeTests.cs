using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Search;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Search;

public sealed class SearchModeTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Constructor_Should_Throw_When_StoreIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new SearchMode(null!));
    }

    [Fact]
    public void Focus_Should_BeInput_When_ModeIsNew()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());

        // assert
        Assert.Equal(SearchFocus.Input, mode.Focus);
    }

    [Fact]
    public async Task TickAsync_Should_LoadAllNonTerminalTasks_When_QueryIsEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        store.Tasks.Add(TaskItemBuilder.Create("t-2"));
        store.Tasks.Add(TaskItemBuilder.Create("t-3", status: TaskStates.Closed));
        var mode = new SearchMode(store);

        // act
        mode.OnEnter();
        var ran = await mode.TickAsync(Now, CancellationToken.None);

        // assert
        Assert.True(ran);
        Assert.Equal(["t-1", "t-2"], mode.Results.Select(t => t.Id));
        Assert.Equal("t-1", mode.SelectedTaskId);
    }

    [Fact]
    public async Task HandleQueryKey_Should_ScheduleDebouncedQuery_When_TextIsValid()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        Assert.Equal(1, store.QueryCount);

        // act
        TypeChar(mode, 'p', Now);
        TypeChar(mode, '0', Now);

        var beforeDue = await mode.TickAsync(Now + SearchMode.DebounceWindow - TimeSpan.FromMilliseconds(1), CancellationToken.None);
        Assert.False(beforeDue);
        Assert.Equal(1, store.QueryCount);

        var afterDue = await mode.TickAsync(Now + SearchMode.DebounceWindow, CancellationToken.None);

        // assert
        Assert.True(afterDue);
        Assert.Equal(2, store.QueryCount);
        Assert.Equal(0, store.LastFilter!.Priority);
        Assert.Null(mode.ParseError);
    }

    [Fact]
    public void HandleQueryKey_Should_SetParseError_When_TextIsInvalid()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());

        // act
        TypeText(mode, "priority:9", Now);

        // assert
        Assert.NotNull(mode.ParseError);
        Assert.Equal("priority:9", mode.QueryText);
    }

    [Fact]
    public async Task TickAsync_Should_NotQuery_When_PendingTextIsInvalid()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        var queriesAfterEntry = store.QueryCount;

        // act
        TypeText(mode, "priority:9", Now);
        var ran = await mode.TickAsync(Now + SearchMode.DebounceWindow, CancellationToken.None);

        // assert
        Assert.False(ran);
        Assert.Equal(queriesAfterEntry, store.QueryCount);
    }

    [Fact]
    public void HandleQueryKey_Should_HaveNoEffect_When_FocusIsNotInput()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.List, mode.Focus);

        // act
        TypeChar(mode, 'x', Now);

        // assert
        Assert.Equal("", mode.QueryText);
    }

    [Fact]
    public void Handle_Should_WalkFocusForward_When_OpenSelectedReceived()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());
        Assert.Equal(SearchFocus.Input, mode.Focus);

        // act & assert: Input -> List
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.List, mode.Focus);

        // act & assert: List -> Detail
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.Detail, mode.Focus);

        // act & assert: Detail stays put
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.Detail, mode.Focus);
    }

    [Fact]
    public void Handle_Should_WalkFocusBack_When_MoveCursorLeftReceived()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());
        mode.Handle(new TuiMessage.OpenSelected());
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.Detail, mode.Focus);

        // act & assert: Detail -> List
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        Assert.Equal(SearchFocus.List, mode.Focus);

        // act & assert: List -> Input
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        Assert.Equal(SearchFocus.Input, mode.Focus);

        // act & assert: Input stays put
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        Assert.Equal(SearchFocus.Input, mode.Focus);
    }

    [Fact]
    public void Handle_Should_FocusDetail_When_MoveCursorRightReceivedOnList()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.List, mode.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(SearchFocus.Detail, mode.Focus);
    }

    [Fact]
    public async Task Handle_Should_MoveSelection_When_FocusIsListAndCursorMoves()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        store.Tasks.Add(TaskItemBuilder.Create("t-2"));
        store.Tasks.Add(TaskItemBuilder.Create("t-3"));
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        mode.Handle(new TuiMessage.OpenSelected());
        Assert.Equal(SearchFocus.List, mode.Focus);
        Assert.Equal("t-1", mode.SelectedTaskId);

        // act & assert
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal("t-2", mode.SelectedTaskId);

        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal("t-3", mode.SelectedTaskId);

        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));
        Assert.Equal("t-2", mode.SelectedTaskId);
    }

    [Fact]
    public async Task Handle_Should_NotMoveSelection_When_FocusIsNotList()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        store.Tasks.Add(TaskItemBuilder.Create("t-2"));
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        Assert.Equal(SearchFocus.Input, mode.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal("t-1", mode.SelectedTaskId);
    }

    [Fact]
    public async Task Handle_Should_MoveSelectionToEdge_When_FocusIsList()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        store.Tasks.Add(TaskItemBuilder.Create("t-2"));
        store.Tasks.Add(TaskItemBuilder.Create("t-3"));
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        mode.Handle(new TuiMessage.OpenSelected());

        // act & assert
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("t-3", mode.SelectedTaskId);

        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));
        Assert.Equal("t-1", mode.SelectedTaskId);
    }

    [Fact]
    public async Task Handle_Should_RerunLastAppliedQuery_When_RefreshRequestedReceived()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1"));
        var mode = new SearchMode(store);
        mode.OnEnter();
        await mode.TickAsync(Now, CancellationToken.None);
        Assert.Equal(1, store.QueryCount);

        // act
        mode.Handle(new TuiMessage.RefreshRequested());
        var ran = await mode.TickAsync(Now, CancellationToken.None);

        // assert
        Assert.True(ran);
        Assert.Equal(2, store.QueryCount);
    }

    [Fact]
    public void KeyMap_Should_MirrorEscapeToMoveLeft_And_TabToOpenSelected()
    {
        // arrange
        var mode = new SearchMode(new FakeTaskStore());

        // act
        var escapeResolved = mode.KeyMap!.TryResolve(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '\u001b'), out var escapeMessage);
        var tabResolved = mode.KeyMap!.TryResolve(
            new KeyChord(ConsoleKey.Tab, ConsoleModifiers.None, '\t'), out var tabMessage);

        // assert
        Assert.True(escapeResolved);
        Assert.Equal(new TuiMessage.MoveCursor(CursorDirection.Left), escapeMessage);
        Assert.True(tabResolved);
        Assert.Equal(new TuiMessage.OpenSelected(), tabMessage);
    }

    private static void TypeChar(SearchMode mode, char c, DateTimeOffset now)
        => mode.HandleQueryKey(new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false), now);

    private static void TypeText(SearchMode mode, string text, DateTimeOffset now)
    {
        foreach (var c in text)
        {
            TypeChar(mode, c, now);
        }
    }
}
