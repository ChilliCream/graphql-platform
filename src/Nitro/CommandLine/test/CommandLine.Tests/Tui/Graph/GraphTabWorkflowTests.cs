using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphTabWorkflowTests
{
    [Fact]
    public void GraphTab_Should_OpenWithCollapsedEpicsAndClosedTasksHidden_When_WorkspaceIsLarge()
    {
        // arrange
        var store = CreateWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateShell(store, graph);
        EnterGraphTab(shell);

        // act
        var rows = graph.TreeView.Rows;

        // assert
        Assert.Equal(["epic-nested", "epic-root"], graph.CollapsedEpicIds.Order(StringComparer.Ordinal));
        Assert.Collection(
            rows,
            row => Assert.Null(row.TaskId),
            row => Assert.Equal("epic-root", row.TaskId),
            row => Assert.Equal("orphan-selected", row.TaskId));
    }

    [Fact]
    public void GraphTab_Should_PreserveSelectionAndCanvasViewport_When_ProjectionRefreshAndDetailRoundTrip()
    {
        // arrange
        var store = CreateWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateShell(store, graph);
        EnterGraphTab(shell);
        graph.SelectTask("orphan-selected");
        shell.Handle(Key('v', ConsoleKey.V));
        _ = RenderToText(shell, width: 60);

        // act
        shell.Handle(Key('v', ConsoleKey.V));
        var treeSelection = graph.SelectedTaskId;
        shell.Handle(Key('v', ConsoleKey.V));
        store.Tasks["layout-seed"] = Task("layout-seed", -1, title: "Seeded relayout task");
        shell.Handle(new TuiEvent.DataChangedEvent());
        var refreshedSelection = graph.SelectedTaskId;
        _ = RenderToText(shell, width: 60);
        var expectedDetailResume = (graph.SelectedTaskId, graph.IsCanvasActive, graph.CanvasView.Viewport);
        var opened = shell.Handle(Key('\r', ConsoleKey.Enter));
        var detail = RenderToText(shell, width: 60);
        var closed = shell.Handle(Key('\x1b', ConsoleKey.Escape));
        _ = RenderToText(shell, width: 60);
        var actualDetailResume = (graph.SelectedTaskId, graph.IsCanvasActive, graph.CanvasView.Viewport);

        // assert
        Assert.Equal("orphan-selected", treeSelection);
        Assert.Equal("orphan-selected", refreshedSelection);
        Assert.True(opened && closed);
        Assert.Contains("Selected orphan task", detail, StringComparison.Ordinal);
        Assert.Equal(expectedDetailResume, actualDetailResume);
    }

    [Fact]
    public void GraphTab_Should_ShowReductionNoticeAndCapRetainedNodes_When_WorkspaceExceedsVisibleNodeCap()
    {
        // arrange
        var store = new FakeTaskStore();

        for (var index = 0; index <= GraphReductionOptions.VisibleNodeCap; index++)
        {
            var id = $"task-{index:D3}";
            store.Tasks[id] = Task(id, index, title: $"Oversized task {index:D3}");
        }

        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateShell(store, graph);
        EnterGraphTab(shell);
        shell.Handle(Key('v', ConsoleKey.V));

        // act
        var output = RenderToText(shell, width: 80);

        // assert
        Assert.Contains("Graph reduced:", output, StringComparison.Ordinal);
        Assert.True(graph.CanvasView.Layout.Nodes.Count <= GraphReductionOptions.VisibleNodeCap);
    }

    [Fact]
    public void BoardTab_Should_RestoreBoardDetail_When_GraphTabIsAlsoHosted()
    {
        // arrange
        var store = CreateWorkspace();
        var board = new BoardMode(
            new BoardDataLoader(store, TimeProvider.System),
            [new BoardView
            {
                Name = "Open",
                Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
            }]);
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = new TuiShell(
            [CreateTab("Tasks", 'T', board), CreateTab("Graph", 'G', graph)],
            80,
            24,
            store: store,
            actor: "tester");
        board.SelectTask("orphan-selected");

        // act
        var opened = shell.Handle(Key('\r', ConsoleKey.Enter));
        var detail = RenderToText(shell);
        var closed = shell.Handle(Key('\x1b', ConsoleKey.Escape));
        var boardOutput = RenderToText(shell);

        // assert
        Assert.True(opened && closed);
        Assert.Contains("Selected orphan task", detail, StringComparison.Ordinal);
        Assert.Contains("Selected orphan task", boardOutput, StringComparison.Ordinal);
    }

    private static TuiShell CreateShell(FakeTaskStore store, GraphMode graph)
        => new(
            [CreateTab("Tasks", 'T', new FakeTuiMode()), CreateTab("Graph", 'G', graph)],
            80,
            24,
            store: store,
            actor: "tester");

    private static TuiTab CreateTab(string title, char mnemonic, ITuiMode mode)
        => new(title, mnemonic, mode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

    private static TuiEvent.KeyEvent Key(char keyChar, ConsoleKey key)
        => new(new ConsoleKeyInfo(keyChar, key, false, false, false));

    private static void EnterGraphTab(TuiShell shell)
        => shell.Handle(new TuiEvent.KeyEvent(new ConsoleKeyInfo('G', ConsoleKey.G, true, false, false)));

    private static string RenderToText(TuiShell shell, int width = 80)
    {
        var console = new TestConsole().Width(width);
        console.Write(shell.Render());
        return console.Output;
    }

    private static FakeTaskStore CreateWorkspace()
    {
        var store = new FakeTaskStore();
        store.Tasks["epic-root"] = Task("epic-root", 0, TaskTypes.Epic, "Root epic");
        store.Tasks["epic-nested"] = Task("epic-nested", 1, TaskTypes.Epic, "Nested epic");
        store.Tasks["nested-child"] = Task("nested-child", 2, title: "Nested child");
        store.Tasks["closed-child"] = Task("closed-child", 3, title: "Closed child", status: TaskStates.Closed);
        store.Tasks["cycle-a"] = Task("cycle-a", 4, title: "Cycle A");
        store.Tasks["cycle-b"] = Task("cycle-b", 5, title: "Cycle B");
        store.Tasks["orphan-selected"] = Task("orphan-selected", 6, title: "Selected orphan task");
        store.DependencyEdges.Add(Parent("epic-root", "epic-nested"));
        store.DependencyEdges.Add(Parent("epic-nested", "nested-child"));
        store.DependencyEdges.Add(Parent("epic-root", "closed-child"));
        store.DependencyEdges.Add(Parent("epic-root", "cycle-a"));
        store.DependencyEdges.Add(Parent("epic-root", "cycle-b"));
        store.DependencyEdges.Add(Blocks("cycle-a", "cycle-b"));
        store.DependencyEdges.Add(Blocks("cycle-b", "cycle-a"));

        for (var index = 0; index < GraphReductionOptions.AdaptiveCollapseThreshold; index++)
        {
            var id = $"child-{index:D3}";
            store.Tasks[id] = Task(id, 10 + index, title: $"Root child {index:D3}");
            store.DependencyEdges.Add(Parent("epic-root", id));
        }

        return store;
    }

    private static TaskItem Task(
        string id,
        int priority,
        string type = TaskTypes.Task,
        string? title = null,
        string status = TaskStates.Open)
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

    private static TaskDependency Parent(string parentId, string childId)
        => Dependency(childId, parentId, TaskDependencyTypes.ParentChild);

    private static TaskDependency Blocks(string blockerId, string dependentId)
        => Dependency(dependentId, blockerId, TaskDependencyTypes.Blocks);

    private static TaskDependency Dependency(string taskId, string dependsOnId, string type)
        => new()
        {
            TaskId = taskId,
            DependsOnId = dependsOnId,
            Type = type,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
}
