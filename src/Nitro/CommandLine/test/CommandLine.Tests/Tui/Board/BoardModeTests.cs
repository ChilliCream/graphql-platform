using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardModeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BoardView TwoColumnView() => new()
    {
        Name = "Test",
        Columns =
        [
            new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] },
            new ColumnDefinition { Name = "Closed", Statuses = [TaskStates.Closed] }
        ]
    };

    private static BoardMode CreateMode(FakeTaskStore store, BoardView? view = null)
        => new(new BoardDataLoader(store, new FakeTimeProvider(Now)), view is null ? null : [view]);

    [Fact]
    public void FocusColumn_Should_ClampAtFirstColumn_When_MovingLeftPastStart()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));

        // assert
        Assert.Equal(0, mode.State.FocusedColumnIndex);
    }

    [Fact]
    public void FocusColumn_Should_ClampAtLastColumn_When_MovingRightPastEnd()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(1, mode.State.FocusedColumnIndex);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.Columns[0].SelectedRow);
    }

    [Fact]
    public void MoveSelectionToEdge_Should_SelectLastRow_When_Bottom()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        store.Tasks.Add(TaskItemBuilder.Create("a-3", status: TaskStates.Open, createdAt: Now.AddDays(2)));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.Columns[0].SelectedRow);
    }

    [Fact]
    public void MoveSelectionToEdge_Should_SelectFirstRow_When_Top()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));

        // assert
        Assert.Equal(0, mode.State.Columns[0].SelectedRow);
    }

    [Fact]
    public void Refresh_Should_PreserveSelectedTask_When_ReloadedTasksReorder()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open, createdAt: Now));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("a-2", mode.State.Columns[0].SelectedTaskId);

        // act: a new, earlier-priority task pushes a-2 to a different row on refresh
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-0", status: TaskStates.Open, priority: TaskPriorities.Critical, createdAt: Now.AddDays(-1)));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal("a-2", mode.State.Columns[0].SelectedTaskId);
        Assert.Equal(2, mode.State.Columns[0].SelectedRow);
    }

    [Fact]
    public void OpenSelected_Should_ReturnDetailViewNotAvailableToast()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal("Detail view not available yet.", toast.Text);
        Assert.Equal(ToastStyle.Info, toast.Style);
    }

    [Fact]
    public void CopySelectedId_Should_ShowIdInToast_When_TaskSelected()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal("a-1", toast.Text);
    }

    [Fact]
    public void CopySelectedId_Should_WarnNoTaskSelected_When_ColumnEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal(ToastStyle.Warn, toast.Style);
    }

    [Fact]
    public void CycleView_Should_ReturnNoMessages_When_OnlySingleBuiltInView()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Empty(messages);
        Assert.Equal("Test", mode.State.View.Name);
    }

    [Fact]
    public void CycleView_Should_SwitchToNextView_When_MultipleViewsConfigured()
    {
        // arrange
        var store = new FakeTaskStore();
        var secondView = new BoardView { Name = "Second", Columns = TwoColumnView().Columns };
        var mode = new BoardMode(
            new BoardDataLoader(store, new FakeTimeProvider(Now)), [TwoColumnView(), secondView]);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal("Second", mode.State.View.Name);
    }

    [Fact]
    public void Render_Should_IncludeColumnTitlesAndTaskBadges()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert
        Assert.Contains("Open (1)", console.Output);
        Assert.Contains("Closed (0)", console.Output);
        Assert.Contains("a-1", console.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var exception = Record.Exception(() => mode.Render(0, 0));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void OnEnter_Should_LoadEveryColumnOfDefaultView_ThroughLoader()
    {
        // arrange: BoardView.Default loaded end-to-end through BoardDataLoader
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("blocked-1", status: TaskStates.Open));
        store.Blocked["blocked-1"] = ["blocker-1"];
        store.Tasks.Add(TaskItemBuilder.Create("deferred-1", status: TaskStates.Deferred));
        store.Tasks.Add(TaskItemBuilder.Create("ready-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("in-progress-1", status: TaskStates.InProgress));
        store.Tasks.Add(TaskItemBuilder.Create(
            "closed-1", status: TaskStates.Closed, closedAt: Now.AddDays(-1)));
        var mode = CreateMode(store, BoardView.Default);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(["blocked-1"], mode.State.Columns[0].Tasks.Select(t => t.Id));
        Assert.Equal(["deferred-1"], mode.State.Columns[1].Tasks.Select(t => t.Id));
        Assert.Equal(["ready-1"], mode.State.Columns[2].Tasks.Select(t => t.Id));
        Assert.Equal(["in-progress-1"], mode.State.Columns[3].Tasks.Select(t => t.Id));
        Assert.Equal(["closed-1"], mode.State.Columns[4].Tasks.Select(t => t.Id));
    }
}
