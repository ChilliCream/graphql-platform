using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardModeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
        => new(new BoardDataLoader(store, new FakeTimeProvider(s_now)), view is null ? null : [view]);

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
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: s_now.AddDays(1)));
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
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: s_now.AddDays(1)));
        store.Tasks.Add(TaskItemBuilder.Create("a-3", status: TaskStates.Open, createdAt: s_now.AddDays(2)));
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
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: s_now.AddDays(1)));
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
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open, createdAt: s_now));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: s_now.AddDays(1)));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("a-2", mode.State.Columns[0].SelectedTaskId);

        // act: a new, earlier-priority task pushes a-2 to a different row on refresh
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-0", status: TaskStates.Open, priority: TaskPriorities.Critical, createdAt: s_now.AddDays(-1)));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal("a-2", mode.State.Columns[0].SelectedTaskId);
        Assert.Equal(2, mode.State.Columns[0].SelectedRow);
    }

    [Fact]
    public void SelectedTaskId_Should_ReturnFocusedColumnsSelectedTask_When_TaskPresent()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var selected = mode.SelectedTaskId;

        // assert
        Assert.Equal("a-1", selected);
    }

    [Fact]
    public void SelectedTaskId_Should_ReturnNull_When_FocusedColumnEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var selected = mode.SelectedTaskId;

        // assert
        Assert.Null(selected);
    }

    [Fact]
    public void SelectedTaskId_Should_FollowFocus_When_FocusedColumnChanges()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Closed));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal("a-2", mode.SelectedTaskId);
    }

    [Fact]
    public void SelectTask_Should_FocusColumnAndSelectRow_When_TaskExists()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Closed));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.SelectTask("a-2");

        // assert
        Assert.Equal(1, mode.State.FocusedColumnIndex);
        Assert.Equal(0, mode.State.Columns[1].SelectedRow);
        Assert.Equal("a-2", mode.SelectedTaskId);
    }

    [Fact]
    public void SelectTask_Should_LeaveSelectionUnchanged_When_TaskDoesNotExist()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        mode.SelectTask("does-not-exist");

        // assert
        Assert.Equal(0, mode.State.FocusedColumnIndex);
        Assert.Equal("a-1", mode.SelectedTaskId);
    }

    [Fact]
    public void OpenSelected_Should_ReturnEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(messages);
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
            new BoardDataLoader(store, new FakeTimeProvider(s_now)), [TwoColumnView(), secondView]);
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
    public void Render_Should_ShowMoreBelowIndicator_When_TasksExceedColumnHeight()
    {
        // arrange
        var store = new FakeTaskStore();
        for (var i = 1; i <= 15; i++)
        {
            store.Tasks.Add(TaskItemBuilder.Create(
                $"t-{i:D2}", status: TaskStates.Open, createdAt: s_now.AddMinutes(i)));
        }

        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(12);

        // act
        console.Write(mode.Render(80, 12));

        // assert
        Assert.Contains("more below", console.Output);
        Assert.DoesNotContain("t-15", console.Output);
    }

    [Fact]
    public void Render_Should_ShowMoreAboveIndicator_And_SelectedTask_When_MovedToBottom()
    {
        // arrange
        var store = new FakeTaskStore();
        for (var i = 1; i <= 15; i++)
        {
            store.Tasks.Add(TaskItemBuilder.Create(
                $"t-{i:D2}", status: TaskStates.Open, createdAt: s_now.AddMinutes(i)));
        }

        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(12);

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        console.Write(mode.Render(80, 12));

        // assert
        Assert.Contains("more above", console.Output);
        Assert.Contains("t-15", console.Output);
    }

    [Fact]
    public void ToggleMaximize_Should_ShowOnlyFocusedColumn_When_Toggled()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(20);

        // act
        mode.Handle(new TuiMessage.ToggleMaximize());
        console.Write(mode.Render(80, 20));

        // assert
        Assert.Contains("Open - 1/2", console.Output);
        Assert.DoesNotContain("Closed", console.Output);
    }

    [Fact]
    public void ToggleMaximize_Should_ShowNewlyFocusedColumn_When_FocusChangedWhileMaximized()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(20);

        // act
        mode.Handle(new TuiMessage.ToggleMaximize());
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));
        console.Write(mode.Render(80, 20));

        // assert
        Assert.Contains("Closed - 2/2", console.Output);
        Assert.DoesNotContain("Open -", console.Output);
    }

    [Fact]
    public void ToggleMaximize_Should_ReturnToGrid_When_ToggledTwice()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(20);

        // act
        mode.Handle(new TuiMessage.ToggleMaximize());
        mode.Handle(new TuiMessage.ToggleMaximize());
        console.Write(mode.Render(80, 20));

        // assert
        Assert.Contains("Open (0)", console.Output);
        Assert.Contains("Closed (0)", console.Output);
    }

    [Fact]
    public void Render_Should_ReClampViewport_When_ResizedSmaller_After_ScrollingDown()
    {
        // arrange
        var store = new FakeTaskStore();
        for (var i = 1; i <= 30; i++)
        {
            store.Tasks.Add(TaskItemBuilder.Create(
                $"t-{i:D2}", status: TaskStates.Open, createdAt: s_now.AddMinutes(i)));
        }

        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var bigConsole = new TestConsole().Width(80).Height(20);
        bigConsole.Write(mode.Render(80, 20));
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        bigConsole.Write(mode.Render(80, 20));

        // act: shrink the frame drastically after scrolling to the bottom; the interior still
        // has room for the header block and a few rows once it shrinks
        mode.OnResize(80, 10);
        var smallConsole = new TestConsole().Width(80).Height(10);
        var exception = Record.Exception(() => smallConsole.Write(mode.Render(80, 10)));

        // assert
        Assert.Null(exception);
        Assert.Contains("t-30", smallConsole.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthIsBelowColumnCount()
    {
        // arrange
        var store = new FakeTaskStore();
        var view = new BoardView
        {
            Name = "Many",
            Columns =
            [
                new ColumnDefinition { Name = "A", Statuses = [TaskStates.Open] },
                new ColumnDefinition { Name = "B", Statuses = [TaskStates.Open] },
                new ColumnDefinition { Name = "C", Statuses = [TaskStates.Open] },
                new ColumnDefinition { Name = "D", Statuses = [TaskStates.Open] },
                new ColumnDefinition { Name = "E", Statuses = [TaskStates.Open] }
            ]
        };
        var mode = CreateMode(store, view);
        mode.OnEnter();
        var console = new TestConsole().Width(3).Height(10);

        // act
        var exception = Record.Exception(() => console.Write(mode.Render(3, 10)));

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
            "closed-1", status: TaskStates.Closed, closedAt: s_now.AddDays(-1)));
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

    [Fact]
    public void Render_Should_FillRequestedHeight_When_Grid()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(24);

        // act
        console.Write(mode.Render(80, 24));

        // assert
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(24, lines.Length);
        Assert.Contains('╰', lines[^1]);
    }

    [Fact]
    public void Render_Should_FillRequestedHeight_When_Maximized()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleMaximize());
        var console = new TestConsole().Width(80).Height(24);

        // act
        console.Write(mode.Render(80, 24));

        // assert
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(24, lines.Length);
        Assert.Contains('╰', lines[^1]);
    }

    [Fact]
    public void Render_Should_FillRequestedHeight_When_Stacked()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(40).Height(24);

        // act
        console.Write(mode.Render(40, 24));

        // assert
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(24, lines.Length);
        Assert.Contains('╰', lines[11]);
        Assert.Equal(string.Empty, lines[12]);
        Assert.Contains('╰', lines[^1]);
    }

    [Fact]
    public void Render_Should_ShowColumnTableHeader_When_ColumnHasTasks()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("TYPE", console.Output);
        Assert.Contains("PRIO", console.Output);
        Assert.Contains("ID", console.Output);
        Assert.Contains("TITLE", console.Output);
    }

    [Fact]
    public void Render_Should_HideTask_When_InteriorHeightExactlyFitsHeaderBlock()
    {
        // arrange: a four-row interior (a six-row maximized panel minus its two chrome rows)
        // is spent entirely on the header block, leaving no room for the column's one task.
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleMaximize());
        var console = new TestConsole().Width(80).Height(6);

        // act
        console.Write(mode.Render(80, 6));

        // assert
        Assert.False(console.Output.Contains("t-1", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_Should_ShowTask_When_InteriorHeightAddsOneRowPastHeaderBlock()
    {
        // arrange: one more interior row than the header block needs, so the column's one
        // task fits below it.
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("t-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleMaximize());
        var console = new TestConsole().Width(80).Height(7);

        // act
        console.Write(mode.Render(80, 7));

        // assert
        Assert.Contains("t-1", console.Output);
    }

    [Fact]
    public void Render_Should_ShowTaskTable_When_GridWithThreeTasks()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-1", status: TaskStates.Open, priority: TaskPriorities.Critical, type: TaskTypes.Bug,
            title: "Fix bug", createdAt: s_now));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-2", status: TaskStates.InProgress, priority: TaskPriorities.Medium, type: TaskTypes.Feature,
            title: "Add feature", createdAt: s_now.AddMinutes(1)));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-3", status: TaskStates.Open, priority: TaskPriorities.Low, type: TaskTypes.Docs,
            title: "Write docs", createdAt: s_now.AddMinutes(2)));
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open, TaskStates.InProgress] }]
        };
        var mode = CreateMode(store, view);
        mode.OnEnter();
        var console = new TestConsole().Width(60).Height(12);

        // act
        console.Write(mode.Render(60, 12));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Open (3)─────────────────────────────────────────────────╮
            │                                                          │
            │     TYPE    PRIO    ID            TITLE                  │
            │ ──────────────────────────────────────────────────────── │
            │                                                          │
            │ > ○ B         P0    a-1           Fix bug                │
            │   ● F         P2    a-2           Add feature            │
            │   ○ D         P3    a-3           Write docs             │
            │                                                          │
            │                                                          │
            │                                                          │
            ╰──────────────────────────────────────────────────────────╯
            """);
    }

    [Fact]
    public void Render_Should_ShowTaskTable_When_MaximizedWithThreeTasks()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-1", status: TaskStates.Open, priority: TaskPriorities.Critical, type: TaskTypes.Bug,
            title: "Fix bug", createdAt: s_now));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-2", status: TaskStates.InProgress, priority: TaskPriorities.Medium, type: TaskTypes.Feature,
            title: "Add feature", createdAt: s_now.AddMinutes(1)));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-3", status: TaskStates.Open, priority: TaskPriorities.Low, type: TaskTypes.Docs,
            title: "Write docs", createdAt: s_now.AddMinutes(2)));
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open, TaskStates.InProgress] }]
        };
        var mode = CreateMode(store, view);
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleMaximize());
        var console = new TestConsole().Width(60).Height(12);

        // act
        console.Write(mode.Render(60, 12));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Open - 1/1 (3)───────────────────────────────────────────╮
            │                                                          │
            │     TYPE    PRIO    ID            TITLE                  │
            │ ──────────────────────────────────────────────────────── │
            │                                                          │
            │ > ○ B         P0    a-1           Fix bug                │
            │   ● F         P2    a-2           Add feature            │
            │   ○ D         P3    a-3           Write docs             │
            │                                                          │
            │                                                          │
            │                                                          │
            ╰──────────────────────────────────────────────────────────╯

            """);
    }

    /// <summary>
    /// Removes the last entry when it is empty; otherwise returns the supplied lines unchanged.
    /// </summary>
    private static string[] TrimTrailingNewline(string[] lines) =>
        lines.Length > 0 && lines[^1].Length == 0 ? lines[..^1] : lines;
}
