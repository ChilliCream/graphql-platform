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

    private static (GraphMode Mode, Mock<ITaskStore> Store) CreateMode(
        List<TaskItem> tasks,
        List<TaskDependency>? dependencies = null)
    {
        var store = new Mock<ITaskStore>();
        store.Setup(t => t.QueryTasksAsync(It.IsAny<TaskFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => tasks.ToArray());
        store.Setup(t => t.GetDependencyEdgesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => dependencies?.ToArray() ?? []);
        store.Setup(t => t.GetLabelsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return (new GraphMode(new GraphDataLoader(store.Object)), store);
    }

    private static TaskItem Task(
        string id,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task,
        string status = TaskStates.Open)
        => new()
        {
            Id = id,
            Title = id,
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
