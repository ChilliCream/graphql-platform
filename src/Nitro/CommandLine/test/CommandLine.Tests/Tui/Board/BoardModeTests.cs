using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
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
    public void SelectedTaskId_Should_ReturnFocusedColumnsSelectedTask_When_TaskPresent()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();

        // act
        var selected = ((ITuiMode)mode).SelectedTaskId;

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
        var selected = ((ITuiMode)mode).SelectedTaskId;

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
        Assert.Equal("a-2", ((ITuiMode)mode).SelectedTaskId);
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
        ((ITuiMode)mode).SelectTask("a-2");

        // assert
        Assert.Equal(1, mode.State.FocusedColumnIndex);
        Assert.Equal(0, mode.State.Columns[1].SelectedRow);
        Assert.Equal("a-2", ((ITuiMode)mode).SelectedTaskId);
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
        ((ITuiMode)mode).SelectTask("does-not-exist");

        // assert
        Assert.Equal(0, mode.State.FocusedColumnIndex);
        Assert.Equal("a-1", ((ITuiMode)mode).SelectedTaskId);
    }

    [Fact]
    public void OpenSelected_Should_ReturnEmpty()
    {
        // arrange: TuiShell intercepts OpenSelected for the board before it
        // ever reaches BoardMode.Handle, switching to BoardDetailMode itself
        // (see TuiShellTests). BoardMode's own handling is a no-op.
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
    public void Render_Should_ApplyLegibleClosedStyle_ToClosedFrameAndTitle_When_Unfocused()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = CreateMode(store, BoardView.Default);
        mode.OnEnter();
        var console = new TestConsole()
            .Colors(ColorSystem.TrueColor)
            .EmitAnsiSequences()
            .Width(150)
            .Height(20);

        // act
        console.Write(mode.Render(150, 20));

        // assert
        var style = ThemeTokens.GetStyle("board.column.status.closed");
        var styleConsole = new TestConsole()
            .Colors(ColorSystem.TrueColor)
            .EmitAnsiSequences()
            .Width(1)
            .Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        var ansiIndex = console.Output.IndexOf(ansiPrefix, StringComparison.Ordinal);
        var textIndex = console.Output.IndexOf("Closed (0)", StringComparison.Ordinal);
        var runStart = ansiIndex + ansiPrefix.Length;

        Assert.Equal(Color.Grey84, style.Foreground);
        Assert.Equal(Decoration.None, style.Decoration);
        Assert.True(
            ansiIndex >= 0 && console.Output[ansiIndex + ansiPrefix.Length] == '╭',
            "Expected the Closed style to begin at its frame.");
        Assert.True(textIndex > ansiIndex, "Expected the Closed title to follow its styled frame.");
        Assert.Equal(-1, console.Output.IndexOf('\u001b', runStart, textIndex - runStart));
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
                $"t-{i:D2}", status: TaskStates.Open, createdAt: Now.AddMinutes(i)));
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
                $"t-{i:D2}", status: TaskStates.Open, createdAt: Now.AddMinutes(i)));
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
                $"t-{i:D2}", status: TaskStates.Open, createdAt: Now.AddMinutes(i)));
        }

        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var bigConsole = new TestConsole().Width(80).Height(20);
        bigConsole.Write(mode.Render(80, 20));
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        bigConsole.Write(mode.Render(80, 20));

        // act: shrink the frame drastically after scrolling to the bottom
        mode.OnResize(80, 4);
        var smallConsole = new TestConsole().Width(80).Height(4);
        var exception = Record.Exception(() => smallConsole.Write(mode.Render(80, 4)));

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

        // assert: every column's bottom border reaches the last requested row,
        // with no blank gap left below the panels.
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
        // arrange: width/columnCount below the stacked threshold forces the
        // stacked layout kind, and 24 rows over 2 columns clears the
        // equal-share minimum, so both columns render expanded.
        var store = new FakeTaskStore();
        var mode = CreateMode(store, TwoColumnView());
        mode.OnEnter();
        var console = new TestConsole().Width(40).Height(24);

        // act
        console.Write(mode.Render(40, 24));

        // assert: the 1 separator row between the 2 columns leaves 23 rows to
        // share, an uneven split of 12 and 11, so the first column's bottom
        // border sits at row 11, row 12 is the blank separator, and the
        // second column's bottom border reaches the last requested row, no
        // blank gap left over below it.
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(24, lines.Length);
        Assert.Contains('╰', lines[11]);
        Assert.Equal(string.Empty, lines[12]);
        Assert.Contains('╰', lines[^1]);
    }

    [Fact]
    public void Render_Should_KeepEveryColumnFrameIntact_When_LongIdsAtNarrowWidth()
    {
        // arrange: BoardView.Default's five columns squeezed into a narrow
        // total width, each holding a task whose id is longer than the
        // column could ever show in full - the exact shape of the reported
        // bug (a row wrapping onto a line the panel never budgeted for).
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("blocked-task-with-a-very-long-identifier", status: TaskStates.Open));
        store.Blocked["blocked-task-with-a-very-long-identifier"] = ["blocker-1"];
        store.Tasks.Add(TaskItemBuilder.Create(
            "deferred-task-with-a-very-long-identifier", status: TaskStates.Deferred));
        store.Tasks.Add(TaskItemBuilder.Create("ready-task-with-a-very-long-identifier", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create(
            "in-progress-task-with-a-very-long-identifier", status: TaskStates.InProgress));
        store.Tasks.Add(TaskItemBuilder.Create(
            "closed-task-with-a-very-long-identifier", status: TaskStates.Closed, closedAt: Now.AddDays(-1)));
        var mode = CreateMode(store, BoardView.Default);
        mode.OnEnter();
        const int width = 120;
        const int height = 20;
        var console = new TestConsole().Width(width).Height(height);

        // act
        console.Write(mode.Render(width, height));

        // assert: the grid fills exactly the requested height, every
        // column's bottom border reaches the last row (no row silently ate
        // an extra line folding an overlong id out of the panel), and no
        // output row overruns the console's width.
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(height, lines.Length);
        Assert.Contains('╰', lines[^1]);
        Assert.All(lines, line => Assert.True(
            line.Length <= width, $"Expected row width <= {width} but was {line.Length}: '{line}'"));
    }

    /// <summary>
    /// Spectre appends a trailing line break to some renderables (a bare
    /// panel, a stacked rows list) but not others (a grid layout), so
    /// splitting console output on '\n' can leave one extra empty entry at
    /// the end. Strips it so line counts are comparable across layout kinds.
    /// </summary>
    private static string[] TrimTrailingNewline(string[] lines) =>
        lines.Length > 0 && lines[^1].Length == 0 ? lines[..^1] : lines;
}
