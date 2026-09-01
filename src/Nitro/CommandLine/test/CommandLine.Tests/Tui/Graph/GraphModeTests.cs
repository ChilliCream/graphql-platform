using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using Moq;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphModeTests
{
    [Fact]
    public void OnEnter_Should_OpenTreeWithClosedTasksHiddenAndBoxedCanvasDefaults()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("epic", priority: 0, type: TaskTypes.Epic),
            Task("child", priority: 1),
            Task("closed", priority: 2, status: TaskStates.Closed)
        };
        var mode = CreateMode(tasks, [Parent("epic", "child")]).Mode;

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(
            (false, true, false, false, "epic"),
            (mode.IsCanvasActive,
                mode.HideClosed,
                mode.CanvasView.IsCompact,
                mode.CanvasView.IncludeParentChild,
                mode.SelectedTaskId));
        Assert.Equal([null, "epic", "child"], mode.TreeView.Rows.Select(t => t.TaskId));
        Assert.Equal(["child", "epic"], mode.CanvasView.Layout.Nodes.Select(t => t.Id).Order(StringComparer.Ordinal));
        Assert.Empty(mode.CollapsedEpicIds);
    }

    [Fact]
    public void OnEnter_Should_ApplyAdaptiveEpicCollapseAboveTheThreshold()
    {
        // arrange
        var tasks = new List<TaskItem> { Task("epic", priority: 0, type: TaskTypes.Epic) };
        var dependencies = new List<TaskDependency>();

        for (var index = 0; index < GraphReductionOptions.AdaptiveCollapseThreshold; index++)
        {
            var id = $"child-{index:D3}";
            tasks.Add(Task(id, priority: 1));
            dependencies.Add(Parent("epic", id));
        }

        var mode = CreateMode(tasks, dependencies).Mode;

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(["epic"], mode.CollapsedEpicIds);
        Assert.Equal([null, "epic"], mode.TreeView.Rows.Select(t => t.TaskId));
        var result = mode.CanvasView.CreateRenderResult();
        Assert.Contains("[epic +60]", result.Buffer.ToText(result.Viewport), StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleProjection_Should_TransferSelectionBothWaysAndKeepItInEachViewport()
    {
        // arrange
        var tasks = new List<TaskItem> { Task("a", priority: 0), Task("b", priority: 1) };
        var mode = CreateMode(tasks, [Blocks("a", "b")]).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        var canvasSelection = mode.CanvasView.SelectedTaskId;
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        _ = mode.Render(10, 3);
        var selected = mode.CanvasView.Layout.FindNode(mode.SelectedTaskId!)!;
        var centerX = selected.X + (selected.Width / 2);
        var centerY = selected.Y + (selected.Height / 2);
        var viewport = mode.CanvasView.Viewport;
        var centered = centerX >= viewport.X
            && centerX < viewport.X + viewport.Width
            && centerY >= viewport.Y
            && centerY < viewport.Y + viewport.Height;
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        var console = new TestConsole().Width(80).Height(1);
        console.Write(mode.Render(80, 1));

        // assert
        Assert.Equal("b", canvasSelection);
        Assert.Equal("a", mode.SelectedTaskId);
        Assert.Equal("a", mode.TreeView.SelectedTaskId);
        Assert.True(centered);
        Assert.Contains("a", console.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void RefreshRequested_Should_ReloadRetainSelectionAndUseDeterministicFallback()
    {
        // arrange
        var tasks = new List<TaskItem> { Task("a", priority: 0), Task("b", priority: 1) };
        var (mode, store) = CreateMode(tasks);
        mode.OnEnter();
        mode.SelectTask("b");
        var tree = mode.TreeView;
        var canvas = mode.CanvasView;

        // act
        mode.Handle(new TuiMessage.RefreshRequested());
        var retained = mode.SelectedTaskId;
        tasks.RemoveAll(t => t.Id == "b");
        tasks.Add(Task("c", priority: 2));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal(("b", "a"), (retained, mode.SelectedTaskId));
        Assert.Same(tree, mode.TreeView);
        Assert.Same(canvas, mode.CanvasView);
        store.Verify(
            t => t.QueryTasksAsync(It.IsAny<TaskFilter>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public void MoveCursor_Should_DelegateEveryDirectionToTheActiveProjection()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("a", priority: 0),
            Task("b", priority: 1),
            Task("c", priority: 2),
            Task("x", priority: 0),
            Task("y", priority: 1),
            Task("z", priority: 2),
            Task("target", priority: 3)
        };
        var mode = CreateMode(
            tasks,
            [Blocks("a", "b"), Blocks("b", "c"), Blocks("x", "target"), Blocks("y", "target"), Blocks("z", "target")]).Mode;
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleGraphProjection());

        // act
        mode.SelectTask("a");
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));
        var right = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        var left = mode.SelectedTaskId;
        mode.SelectTask("y");
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));
        var up = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        var down = mode.SelectedTaskId;
        mode.SelectTask("target");
        var cycle = new string[3];
        for (var index = 0; index < cycle.Length; index++)
        {
            mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
            cycle[index] = mode.SelectedTaskId!;
        }

        // assert
        Assert.Equal(("b", "a", "x", "z"), (right, left, up, down));
        Assert.Equal(["x", "y", "z"], cycle);
    }

    [Fact]
    public void MoveCursor_Should_UseTreeSelectionAndCollapseSemantics_When_TreeIsActive()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("epic", priority: 0, type: TaskTypes.Epic),
            Task("child", priority: 1)
        };
        var mode = CreateMode(tasks, [Parent("epic", "child")]).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        var down = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));
        var up = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        var collapsedRows = RowsText(mode.TreeView.Rows.Select(t => t.TaskId));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(("child", "epic"), (down, up));
        Assert.Equal("<root>,epic", collapsedRows);
        Assert.Equal("<root>,epic,child", RowsText(mode.TreeView.Rows.Select(t => t.TaskId)));
        Assert.Empty(mode.CollapsedEpicIds);
    }

    [Fact]
    public void Handle_Should_ToggleCanvasPresentationWithoutChangingProjection()
    {
        // arrange
        var mode = CreateMode([Task("task")]).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.ToggleGraphCompact());
        mode.Handle(new TuiMessage.ToggleGraphParentChild());

        // assert
        Assert.Equal((false, true, true),
            (mode.IsCanvasActive, mode.CanvasView.IsCompact, mode.CanvasView.IncludeParentChild));
    }

    [Fact]
    public void Handle_Should_CollapseAndExpandSelectedAndAllEpicsWithoutUsingEnter()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("epic-a", priority: 0, type: TaskTypes.Epic),
            Task("child-a", priority: 1),
            Task("epic-b", priority: 2, type: TaskTypes.Epic),
            Task("child-b", priority: 3)
        };
        var mode = CreateMode(tasks, [Parent("epic-a", "child-a"), Parent("epic-b", "child-b")]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic-a");

        // act
        mode.Handle(new TuiMessage.OpenSelected());
        var enterRows = mode.TreeView.Rows.Select(t => t.TaskId).ToArray();
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        var selectedRows = mode.TreeView.Rows.Select(t => t.TaskId).ToArray();
        mode.Handle(new TuiMessage.ExpandSelectedGraphEpic());
        mode.SelectTask("child-b");
        mode.Handle(new TuiMessage.CollapseAllGraphEpics());
        var allCollapsed = mode.CollapsedEpicIds.Order(StringComparer.Ordinal).ToArray();
        var superNodeSelection = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.ExpandAllGraphEpics());

        // assert
        Assert.Equal("<root>,epic-a,child-a,epic-b,child-b", RowsText(enterRows));
        Assert.Equal("<root>,epic-a,epic-b,child-b", RowsText(selectedRows));
        Assert.Equal(["epic-a", "epic-b"], allCollapsed);
        Assert.Equal("epic-b", superNodeSelection);
        Assert.Empty(mode.CollapsedEpicIds);
    }

    [Fact]
    public void ToggleClosed_Should_ApplyTheSameVisibilityAndSelectionFallbackToBothProjections()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("open", priority: 0),
            Task("closed", priority: 1, status: TaskStates.Closed)
        };
        var mode = CreateMode(tasks).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.ToggleGraphClosed());
        mode.SelectTask("closed");
        var shown = (
            Tree: mode.TreeView.Rows.Select(t => t.TaskId).ToArray(),
            Canvas: mode.CanvasView.Layout.Nodes.Select(t => t.Id).ToArray(),
            Selection: mode.SelectedTaskId);
        mode.Handle(new TuiMessage.ToggleGraphClosed());

        // assert
        Assert.Equal("<root>,open,closed", RowsText(shown.Tree));
        Assert.Equal(["closed", "open"], shown.Canvas.Order(StringComparer.Ordinal));
        Assert.Equal("closed", shown.Selection);
        Assert.Equal(("open", "open"), (mode.TreeView.SelectedTaskId, mode.CanvasView.SelectedTaskId));
    }

    [Fact]
    public void CollapseSelected_Should_KeepBothViewsExpanded_When_OnlyChildrenAreHidden()
    {
        // arrange
        var tasks = new List<TaskItem>
        {
            Task("epic", priority: 0, type: TaskTypes.Epic),
            Task("closed", priority: 1, status: TaskStates.Closed)
        };
        var dependencies = new List<TaskDependency> { Parent("epic", "closed") };
        var (mode, _) = CreateMode(tasks, dependencies);
        mode.OnEnter();
        mode.SelectTask("epic");

        // act
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        mode.Handle(new TuiMessage.ToggleGraphClosed());
        tasks.Add(Task("fresh", priority: 2));
        dependencies.Add(Parent("epic", "fresh"));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Empty(mode.CollapsedEpicIds);
        Assert.Equal([null, "epic", "closed", "fresh"], mode.TreeView.Rows.Select(t => t.TaskId));
        Assert.Equal(["closed", "epic", "fresh"], mode.CanvasView.Layout.Nodes.Select(t => t.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void CollapseAll_Should_KeepAnEpicCollapsedUntilAVisibleChildCanBeExpanded()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("epic", priority: 0, type: TaskTypes.Epic),
                Task("closed", priority: 1, status: TaskStates.Closed)
            ],
            [Parent("epic", "closed")]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic");

        // act
        mode.Handle(new TuiMessage.CollapseAllGraphEpics());
        mode.Handle(new TuiMessage.ExpandSelectedGraphEpic());
        var hiddenState = mode.CollapsedEpicIds.ToArray();
        mode.Handle(new TuiMessage.ToggleGraphClosed());
        var shownRows = mode.TreeView.Rows.Select(t => t.TaskId).ToArray();
        mode.Handle(new TuiMessage.ExpandSelectedGraphEpic());

        // assert
        Assert.Equal(["epic"], hiddenState);
        Assert.Equal(new string?[] { null, "epic" }, shownRows);
        Assert.Empty(mode.CollapsedEpicIds);
    }

    [Fact]
    public void CollapseSelectedGraphEpic_Should_CollapseTheNearestCanvasEpic_When_TreeTaskRowDoesNotCollapse()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("outer", priority: 0, type: TaskTypes.Epic),
                Task("inner", priority: 1, type: TaskTypes.Epic),
                Task("child", priority: 2)
            ],
            [Parent("outer", "inner"), Parent("inner", "child")]).Mode;
        mode.OnEnter();
        mode.SelectTask("child");

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        var treeState = mode.CollapsedEpicIds.ToArray();
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        var canvasState = mode.CollapsedEpicIds.ToArray();
        mode.Handle(new TuiMessage.ExpandSelectedGraphEpic());

        // assert
        Assert.Empty(treeState);
        Assert.Equal(["inner"], canvasState);
        Assert.Equal("inner", mode.SelectedTaskId);
        Assert.Empty(mode.CollapsedEpicIds);
    }

    [Fact]
    public void CollapseSelectedGraphEpic_Should_UseTheCanonicalPriorityParent_When_CollapsingFromCanvas()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("z", priority: 0, type: TaskTypes.Epic),
                Task("a", priority: 4, type: TaskTypes.Epic),
                Task("child", priority: 2)
            ],
            [Parent("a", "child"), Parent("z", "child")]).Mode;
        mode.OnEnter();
        mode.SelectTask("child");
        mode.Handle(new TuiMessage.ToggleGraphProjection());

        // act
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());

        // assert
        Assert.Equal(["z"], mode.CollapsedEpicIds);
        Assert.Equal("z", mode.SelectedTaskId);
        Assert.Equal(["a", "z"], mode.CanvasView.Layout.Nodes.Select(t => t.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ToggleProjection_Should_SelectTheForcedCanvasRepresentative_When_TheTreeChildExceedsTheCap()
    {
        // arrange
        var tasks = new List<TaskItem> { Task("epic", priority: 0, type: TaskTypes.Epic) };
        var dependencies = new List<TaskDependency>();

        for (var index = 0; index < GraphReductionOptions.VisibleNodeCap; index++)
        {
            var id = $"child-{index:D3}";
            tasks.Add(Task(id, priority: 1));
            dependencies.Add(Parent("epic", id));
        }

        var mode = CreateMode(tasks, dependencies).Mode;
        mode.OnEnter();
        mode.Handle(new TuiMessage.ExpandAllGraphEpics());
        mode.SelectTask("child-399");

        // act
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        _ = mode.Render(20, 4);

        // assert
        Assert.Equal("epic", mode.SelectedTaskId);
        Assert.Equal("epic", mode.CanvasView.Layout.FindNode(mode.SelectedTaskId!)?.Id);
        Assert.Equal("epic", mode.CanvasView.SelectedTaskId);
    }

    [Fact]
    public void Search_Should_HighlightMatchesWithoutExpandingCollapsedEpicsOrRelayingOutCanvas()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("epic", priority: 0, type: TaskTypes.Epic),
                Task("child", priority: 1, title: "Find this task")
            ],
            [Parent("epic", "child")]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic");
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        var before = mode.CanvasView.Layout.Nodes.Select(t => (t.Id, t.X, t.Y)).ToArray();

        // act
        mode.Handle(new TuiMessage.FocusSearchRequested());
        Type(mode, "find");
        var first = mode.HandleRawKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        var after = mode.CanvasView.Layout.Nodes.Select(t => (t.Id, t.X, t.Y)).ToArray();

        // assert
        Assert.True(mode.IsInputCapturing);
        Assert.Equal(1, mode.TreeView.Rows.Single(t => t.TaskId == "epic").ContainedMatchCount);
        Assert.Equal("epic", mode.SelectedTaskId);
        Assert.Empty(first);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Search_Should_CycleUniqueCollapsedRepresentativesAcrossProjectionChanges()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("epic", priority: 0, type: TaskTypes.Epic),
                Task("child", priority: 1, title: "find child"),
                Task("child-two", priority: 2, title: "find child two"),
                Task("other", priority: 3, title: "find other")
            ],
            [Parent("epic", "child"), Parent("epic", "child-two")]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic");
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        mode.Handle(new TuiMessage.FocusSearchRequested());
        Type(mode, "find");

        // act
        mode.HandleRawKey(EnterKey());
        var first = mode.SelectedTaskId;
        mode.HandleRawKey(EnterKey());
        var second = mode.SelectedTaskId;
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        mode.HandleRawKey(EnterKey());
        var third = mode.SelectedTaskId;

        // assert
        Assert.Equal(("epic", "other", "epic"), (first, second, third));
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void Search_Should_PreserveCanvasLayoutViewportAndHorizontalCycle_When_QueryChanges()
    {
        // arrange
        var mode = CreateMode(
            [
                Task("origin", priority: 0, title: "find origin"),
                Task("target-a", priority: 1),
                Task("target-b", priority: 2)
            ],
            [Blocks("origin", "target-a"), Blocks("origin", "target-b")]).Mode;
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        mode.SelectTask("origin");
        _ = mode.Render(12, 3);
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));
        var layout = mode.CanvasView.Layout.Nodes.Select(t => (t.Id, t.X, t.Y)).ToArray();
        var viewport = mode.CanvasView.Viewport;

        // act
        mode.Handle(new TuiMessage.FocusSearchRequested());
        Type(mode, "find");
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(layout, mode.CanvasView.Layout.Nodes.Select(t => (t.Id, t.X, t.Y)));
        Assert.Equal(viewport, mode.CanvasView.Viewport);
        Assert.Equal("target-b", mode.SelectedTaskId);
    }

    [Fact]
    public void FilterForm_Should_ApplyAndClearGraphFiltersWithoutChangingTheSource()
    {
        // arrange
        var labels = new[]
        {
            new TaskLabels("one", ["alpha"]),
            new TaskLabels("two", ["beta"])
        };
        var mode = CreateMode([Task("one"), Task("two")], taskLabels: labels).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.FilterGraphRequested());
        Type(mode, "alpha");
        mode.HandleRawKey(new ConsoleKeyInfo('s', ConsoleKey.S, false, false, true));
        var filtered = mode.CanvasView.Layout.Nodes.Select(t => t.Id).ToArray();
        mode.Handle(new TuiMessage.FilterGraphRequested());
        mode.HandleRawKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        mode.HandleRawKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        mode.HandleRawKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        mode.HandleRawKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        // assert
        Assert.Equal(["one"], filtered);
        Assert.Equal(["one", "two"], mode.CanvasView.Layout.Nodes.Select(t => t.Id).Order(StringComparer.Ordinal));
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void FilterForm_Should_KeepTreeCanvasSearchAndSelectionConsistentBehindClosedIntermediates()
    {
        // arrange
        var labels = new[] { new TaskLabels("descendant", ["alpha"]) };
        var mode = CreateMode(
            [
                Task("epic", priority: 0, type: TaskTypes.Epic),
                Task("middle", priority: 1, status: TaskStates.Closed),
                Task("descendant", priority: 2, title: "find descendant")
            ],
            [Parent("epic", "middle"), Parent("middle", "descendant")],
            labels).Mode;
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.FilterGraphRequested());
        Type(mode, "alpha");
        mode.HandleRawKey(TabKey());
        Type(mode, "epic");
        mode.HandleRawKey(SaveKey());
        mode.Handle(new TuiMessage.FocusSearchRequested());
        Type(mode, "find");
        mode.HandleRawKey(EnterKey());

        // assert
        Assert.Equal([null, "descendant"], mode.TreeView.Rows.Select(t => t.TaskId));
        Assert.Equal(["descendant"], mode.CanvasView.Layout.Nodes.Select(t => t.Id));
        Assert.Equal("descendant", mode.SelectedTaskId);
    }

    [Fact]
    public void FilterForm_Should_RetainManualCollapseAcrossApplyAndClear()
    {
        // arrange
        var mode = CreateMode(
            [Task("epic", type: TaskTypes.Epic), Task("child")],
            [Parent("epic", "child")],
            [new TaskLabels("child", ["alpha"])]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic");
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());

        // act
        mode.Handle(new TuiMessage.FilterGraphRequested());
        Type(mode, "alpha");
        mode.HandleRawKey(SaveKey());
        mode.Handle(new TuiMessage.FilterGraphRequested());
        mode.HandleRawKey(TabKey());
        mode.HandleRawKey(TabKey());
        mode.HandleRawKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        mode.HandleRawKey(EnterKey());

        // assert
        Assert.Equal(["epic"], mode.CollapsedEpicIds);
        Assert.Equal([null, "epic"], mode.TreeView.Rows.Select(t => t.TaskId));
    }

    [Fact]
    public void FilterForm_Should_PrefillCancelAndShowTheActiveFilterNotice()
    {
        // arrange
        var mode = CreateMode([Task("task")], taskLabels: [new TaskLabels("task", ["alpha"])]).Mode;
        mode.OnEnter();
        mode.Handle(new TuiMessage.FilterGraphRequested());
        Type(mode, "alpha");
        mode.HandleRawKey(SaveKey());
        var filtered = Render(mode);

        // act
        mode.Handle(new TuiMessage.FilterGraphRequested());
        var form = Render(mode);
        mode.HandleRawKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

        // assert
        Assert.Contains("Filters active", filtered, StringComparison.Ordinal);
        Assert.Contains("alpha", form, StringComparison.Ordinal);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void RefreshRequested_Should_ReapplyActiveGraphSearchFiltersAndManualCollapse()
    {
        // arrange
        var mode = CreateMode(
            [Task("epic", type: TaskTypes.Epic), Task("child", title: "find child")],
            [Parent("epic", "child")],
            [new TaskLabels("child", ["alpha"])]).Mode;
        mode.OnEnter();
        mode.SelectTask("epic");
        mode.Handle(new TuiMessage.CollapseSelectedGraphEpic());
        mode.Handle(new TuiMessage.FilterGraphRequested());
        Type(mode, "alpha");
        mode.HandleRawKey(TabKey());
        Type(mode, "epic");
        mode.HandleRawKey(SaveKey());
        mode.Handle(new TuiMessage.ToggleGraphProjection());
        mode.Handle(new TuiMessage.FocusSearchRequested());
        Type(mode, "find");
        mode.HandleRawKey(EnterKey());

        // act
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.True(mode.IsCanvasActive && mode.HideClosed);
        Assert.Equal(["child"], mode.CanvasView.Layout.Nodes.Select(t => t.Id));
        Assert.Equal("child", mode.SelectedTaskId);
        Assert.Equal(["epic"], mode.CollapsedEpicIds);
    }

    private static (GraphMode Mode, Mock<ITaskStore> Store) CreateMode(
        List<TaskItem> tasks,
        List<TaskDependency>? dependencies = null,
        IReadOnlyList<TaskLabels>? taskLabels = null)
    {
        var store = new Mock<ITaskStore>();
        store.Setup(t => t.QueryTasksAsync(It.IsAny<TaskFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => tasks.ToArray());
        store.Setup(t => t.GetDependencyEdgesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => dependencies?.ToArray() ?? []);
        store.Setup(t => t.GetLabelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        store.Setup(t => t.GetTaskLabelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => taskLabels ?? []);
        return (new GraphMode(new GraphDataLoader(store.Object)), store);
    }

    private static void Type(GraphMode mode, string value)
    {
        foreach (var character in value)
        {
            mode.HandleRawKey(new ConsoleKeyInfo(character, ConsoleKey.A, false, false, false));
        }
    }

    private static ConsoleKeyInfo EnterKey() => new('\r', ConsoleKey.Enter, false, false, false);

    private static ConsoleKeyInfo TabKey() => new('\t', ConsoleKey.Tab, false, false, false);

    private static ConsoleKeyInfo SaveKey() => new('s', ConsoleKey.S, false, false, true);

    private static string Render(GraphMode mode)
    {
        var console = new TestConsole().Width(80).Height(20);
        console.Write(mode.Render(80, 20));
        return console.Output;
    }

    private static TaskItem Task(
        string id,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string status = TaskStates.Open,
        string? title = null)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Priority = priority,
            Type = type,
            Status = status,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

    private static TaskDependency Blocks(string blockerId, string dependentId)
        => Dependency(dependentId, blockerId, TaskDependencyTypes.Blocks);

    private static TaskDependency Parent(string parentId, string childId)
        => Dependency(childId, parentId, TaskDependencyTypes.ParentChild);

    private static TaskDependency Dependency(string taskId, string dependsOnId, string type)
        => new()
        {
            TaskId = taskId,
            DependsOnId = dependsOnId,
            Type = type,
            CreatedAt = DateTimeOffset.UnixEpoch
        };

    private static string RowsText(IEnumerable<string?> ids)
        => string.Join(',', ids.Select(t => t ?? "<root>"));
}
