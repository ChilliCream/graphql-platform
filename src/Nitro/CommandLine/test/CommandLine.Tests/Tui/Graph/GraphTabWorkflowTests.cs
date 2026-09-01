using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphTabWorkflowTests
{
    [Fact]
    public void GraphTab_Should_OpenTreeWithCollapsedEpicsAndClosedTasksHidden_When_EnteredFromTasks()
    {
        // arrange
        var store = CreateLargeGraphWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateGraphShell(store, graph, height: 8);

        // act
        var switched = EnterGraphTab(shell);
        var output = RenderToText(shell, height: 8);

        // assert
        Assert.True(switched);
        Assert.Equal((false, true), (graph.IsCanvasActive, graph.HideClosed));
        Assert.Equal(
            [null, "epic-root", "cycle-a", "cycle-b", "top-level-open", "orphan-selected"],
            graph.TreeView.Rows.Select(row => row.TaskId));
        Assert.Equal(["epic-nested", "epic-root"], graph.CollapsedEpicIds.Order(StringComparer.Ordinal));
        NormalizeFrame(output).MatchInlineSnapshot(
            """
             [T]asks   [G]raph
            Root
            ├─ ▸ ○ [E] Root epic epic-root  blocked by 0 / blocks 0
            ├─   ○ [T] Cycle A cycle-a  blocked by 1 / blocks 1
            ├─   ○ [T] Cycle B cycle-b  blocked by 1 / blocks 1
            ├─   ○ [T] Top-level open task top-level-open  blocked by 0 / blocks 0
            └─   ○ [T] Selected orphan task orphan-selected  blocked by 0 / blocks 0
            """);
    }

    [Fact]
    public void GraphTab_Should_RenderCycleEdgesWhenEpicsAreCollapsed_InCanvas()
    {
        // arrange
        var store = CreateLargeGraphWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateGraphShell(store, graph);
        EnterGraphTab(shell);

        // act
        shell.Handle(Key('v', ConsoleKey.V));
        var output = RenderToText(shell, height: 8);
        var footer = GraphRenderFooter.CreateText(graph.CanvasView.CreateRenderResult());

        // assert
        Assert.Equal(
            ["cycle-a", "cycle-b", "epic-root", "orphan-selected", "top-level-open"],
            graph.CanvasView.Layout.Nodes.Select(node => node.Id).Order(StringComparer.Ordinal));
        Assert.Equal(1, graph.CanvasView.Layout.ReversedEdgeCount);
        footer.MatchInlineSnapshot("nodes: 5  edges: 2  grid: 64 x 19  crossings: 0  reversed: 1");
        NormalizeFrame(output).MatchInlineSnapshot(
            """
             [T]asks   [G]raph
            ┌────────────────────────────┐◀┄┄─┌────────────────────────────┐
            │○ [T] cycle-a               │───▶│○ [T] cycle-b               │
            │Cycle A                     │    │Cycle B                     │
            └────────────────────────────┘    └────────────────────────────┘

            ┌────────────────────────────┐
            │○ [E] epic-root             │
            """);
    }

    [Fact]
    public void GraphTab_Should_PreserveSelection_When_ProjectionIsFlippedRoundTrip()
    {
        // arrange
        var store = CreateLargeGraphWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateGraphShell(store, graph, height: 8);
        EnterGraphTab(shell);
        graph.SelectTask("orphan-selected");

        // act
        var flippedToCanvas = shell.Handle(Key('v', ConsoleKey.V));
        var canvas = RenderToText(shell, height: 8);
        var canvasState = (graph.SelectedTaskId, graph.IsCanvasActive);
        var flippedToTree = shell.Handle(Key('v', ConsoleKey.V));
        var tree = RenderToText(shell, height: 8);
        var treeState = (graph.SelectedTaskId, graph.IsCanvasActive);

        // assert
        Assert.True(flippedToCanvas && flippedToTree);
        Assert.Equal(("orphan-selected", true), canvasState);
        Assert.Equal(("orphan-selected", false), treeState);
        new[] { NormalizeFrame(canvas), NormalizeFrame(tree) }.MatchInlineSnapshots(
            [
                """
                 [T]asks   [G]raph
                ┌────────────────────────────┐
                │○ [T] orphan-selected       │
                │Selected orphan task        │
                └────────────────────────────┘

                nodes: 5  edges: 2  grid: 64 x 19  crossings: 0  reversed: 1
                """,
                """
                 [T]asks   [G]raph
                Root
                ├─ ▸ ○ [E] Root epic epic-root  blocked by 0 / blocks 0
                ├─   ○ [T] Cycle A cycle-a  blocked by 1 / blocks 1
                ├─   ○ [T] Cycle B cycle-b  blocked by 1 / blocks 1
                ├─   ○ [T] Top-level open task top-level-open  blocked by 0 / blocks 0
                └─   ○ [T] Selected orphan task orphan-selected  blocked by 0 / blocks 0
                """
            ]);
    }

    [Fact]
    public void GraphTab_Should_RetainSelectionAndReloadCanvas_When_RefreshIsPressedAfterStoreChanges()
    {
        // arrange
        var store = CreateRefreshWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateGraphShell(store, graph);
        EnterGraphTab(shell);
        shell.Handle(Key('v', ConsoleKey.V));
        graph.SelectTask("orphan-selected");
        _ = RenderToText(shell, width: 48);
        store.Tasks["layout-seed"] = Task("layout-seed", -1, title: "Seeded relayout task");

        // act
        var refreshed = shell.Handle(Key('r', ConsoleKey.R));
        _ = RenderToText(shell, width: 48);

        // assert
        Assert.True(refreshed);
        Assert.Equal(
            ["layout-seed", "orphan-selected", "refresh-source", "refresh-target"],
            graph.CanvasView.Layout.Nodes.Select(node => node.Id).Order(StringComparer.Ordinal));
        Assert.Equal("orphan-selected", graph.SelectedTaskId);
    }

    [Fact]
    public void GraphTab_Should_RestoreCanvasViewport_When_DetailIsClosed()
    {
        // arrange
        var store = CreateDetailWorkspace();
        var graph = new GraphMode(new GraphDataLoader(store));
        var shell = CreateGraphShell(store, graph, width: 40, height: 8);
        EnterGraphTab(shell);
        shell.Handle(Key('v', ConsoleKey.V));
        graph.SelectTask("orphan-selected");
        _ = RenderToText(shell, width: 40);
        var before = (graph.SelectedTaskId, graph.IsCanvasActive, graph.CanvasView.Viewport);

        // act
        var opened = shell.Handle(Key('\r', ConsoleKey.Enter));
        var detail = RenderToText(shell, width: 40, height: 8);
        var closed = shell.Handle(Key('\x1b', ConsoleKey.Escape));
        var resumed = RenderToText(shell, width: 40, height: 8);
        var after = (graph.SelectedTaskId, graph.IsCanvasActive, graph.CanvasView.Viewport);

        // assert
        Assert.True(opened);
        Assert.True(closed);
        Assert.True(before.Viewport.X > 0 || before.Viewport.Y > 0);
        Assert.Equal(before, after);
        new[] { NormalizeFrame(detail), NormalizeFrame(resumed) }.MatchInlineSnapshots(
            [
                """
                 [T]asks   [G]raph
                ╭─orphan-selected Selected orphan task─╮
                │             No details.              │
                ╰──────────────────────────────────────╯
                ╭─Details──────────────────────────────╮
                │ ○ open                               │
                ╰──────────────────────────────────────╯
                """,
                """
                 [T]asks   [G]raph
                ┌────────────────────────────┐
                │○ [T] orphan-selected       │
                │Selected orphan task        │
                └────────────────────────────┘

                nodes: 15  edges: 1  grid: 64 x 69  cro…
                """
            ]);
    }

    [Fact]
    public void BoardTab_Should_RestoreBoardOutputAndSelection_When_DetailIsClosed()
    {
        // arrange
        var store = CreateBoardWorkspace();
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
            8,
            store: store,
            actor: "tester");
        board.SelectTask("board-selected");

        // act
        var opened = shell.Handle(Key('\r', ConsoleKey.Enter));
        var detail = RenderToText(shell, height: 8);
        var closed = shell.Handle(Key('\x1b', ConsoleKey.Escape));
        var boardOutput = RenderToText(shell, height: 8);

        // assert
        Assert.True(opened);
        Assert.True(closed);
        Assert.Equal("board-selected", board.SelectedTaskId);
        new[] { NormalizeFrame(detail), NormalizeFrame(boardOutput) }.MatchInlineSnapshots(
            [
                """
                 [T]asks   [G]raph
                ╭─board-selected Board selected task───────────────────────────────────────────╮
                │                                 No details.                                  │
                ╰──────────────────────────────────────────────────────────────────────────────╯
                ╭─Details──────────────────────────────────────────────────────────────────────╮
                │ ○ open                                                                       │
                ╰──────────────────────────────────────────────────────────────────────────────╯
                """,
                """
                 [T]asks   [G]raph
                ╭─Open (2)─────────────────────────────────────────────────────────────────────╮
                │ > ○ [T] P0 board-selected Board selected task                                │
                │   ○ [T] P1 board-companion Board-only companion                              │
                │                                                                              │
                │                                                                              │
                ╰──────────────────────────────────────────────────────────────────────────────╯
                """
            ]);
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
        var shell = CreateGraphShell(store, graph);
        EnterGraphTab(shell);
        shell.Handle(Key('v', ConsoleKey.V));

        // act
        var output = RenderToText(shell, width: 80);

        // assert
        Assert.Contains("Graph reduced:", output, StringComparison.Ordinal);
        Assert.True(graph.CanvasView.Layout.Nodes.Count <= GraphReductionOptions.VisibleNodeCap);
    }

    private static TuiShell CreateGraphShell(FakeTaskStore store, GraphMode graph, int width = 80, int height = 24)
        => new(
            [CreateTab("Tasks", 'T', new FakeTuiMode()), CreateTab("Graph", 'G', graph)],
            width,
            height,
            store: store,
            actor: "tester");

    private static TuiTab CreateTab(string title, char mnemonic, ITuiMode mode)
        => new(title, mnemonic, mode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

    private static TuiEvent.KeyEvent Key(char keyChar, ConsoleKey key)
        => new(new ConsoleKeyInfo(keyChar, key, false, false, false));

    private static bool EnterGraphTab(TuiShell shell)
        => shell.Handle(new TuiEvent.KeyEvent(new ConsoleKeyInfo('G', ConsoleKey.G, true, false, false)));

    private static string RenderToText(TuiShell shell, int width = 80, int height = 24)
    {
        var console = new TestConsole().Width(width).Height(height);
        console.Write(shell.Render());
        return console.Output;
    }

    private static string NormalizeFrame(string frame)
        => string.Join('\n', frame.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => line.TrimEnd())).TrimEnd();

    private static FakeTaskStore CreateLargeGraphWorkspace()
    {
        var store = new FakeTaskStore();
        store.Tasks["epic-root"] = Task("epic-root", 0, TaskTypes.Epic, "Root epic");
        store.Tasks["epic-nested"] = Task("epic-nested", 1, TaskTypes.Epic, "Nested epic");
        store.Tasks["nested-child"] = Task("nested-child", 2, title: "Nested child");
        store.Tasks["closed-child"] = Task("closed-child", 3, title: "Closed child", status: TaskStates.Closed);
        store.Tasks["cycle-a"] = Task("cycle-a", 4, title: "Cycle A");
        store.Tasks["cycle-b"] = Task("cycle-b", 5, title: "Cycle B");
        store.Tasks["top-level-open"] = Task("top-level-open", 6, title: "Top-level open task");
        store.Tasks["top-level-closed"] = Task("top-level-closed", 7, title: "Top-level closed task", status: TaskStates.Closed);
        store.Tasks["orphan-selected"] = Task("orphan-selected", 8, title: "Selected orphan task");
        store.DependencyEdges.Add(Parent("epic-root", "epic-nested"));
        store.DependencyEdges.Add(Parent("epic-nested", "nested-child"));
        store.DependencyEdges.Add(Parent("epic-root", "closed-child"));
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

    private static FakeTaskStore CreateRefreshWorkspace()
    {
        var store = new FakeTaskStore();
        store.Tasks["refresh-source"] = Task("refresh-source", 0, title: "Refresh source");
        store.Tasks["refresh-target"] = Task("refresh-target", 1, title: "Refresh target");
        store.Tasks["orphan-selected"] = Task("orphan-selected", 2, title: "Selected orphan task");
        store.DependencyEdges.Add(Blocks("refresh-source", "refresh-target"));
        return store;
    }

    private static FakeTaskStore CreateDetailWorkspace()
    {
        var store = CreateRefreshWorkspace();

        for (var index = 0; index < 12; index++)
        {
            var id = $"detail-{index:D2}";
            store.Tasks[id] = Task(id, 10 + index, title: $"Detail task {index:D2}");
        }

        return store;
    }

    private static FakeTaskStore CreateBoardWorkspace()
    {
        var store = new FakeTaskStore();
        store.Tasks["board-selected"] = Task("board-selected", 0, title: "Board selected task");
        store.Tasks["board-companion"] = Task("board-companion", 1, title: "Board-only companion");
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
