using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.TreeView;

public sealed class GraphTreeViewTests
{
    [Fact]
    public void Create_Should_BuildRootSpineForNestedEpicsAndOrphanTasks()
    {
        // arrange
        var model = Model(
            [
                Node("epic", type: TaskTypes.Epic, priority: 0),
                Node("nested", type: TaskTypes.Epic, priority: 1),
                Node("nested-task", priority: 1),
                Node("task", priority: 2),
                Node("orphan", priority: 3)
            ],
            [
                Edge("epic", "nested", GraphEdgeKind.ParentChild),
                Edge("nested", "nested-task", GraphEdgeKind.ParentChild),
                Edge("epic", "task", GraphEdgeKind.ParentChild)
            ]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        Assert.Equal([null, "epic", "nested", "nested-task", "task", "orphan"], view.Rows.Select(t => t.TaskId));
        Assert.Equal([0, 1, 2, 3, 2, 1], view.Rows.Select(t => t.Connector.Depth));
        Assert.Equal("├─ ", view.Rows[1].Connector.BuildConnector());
        Assert.Equal("│  ├─ ", view.Rows[2].Connector.BuildConnector());
        Assert.Equal("└─ ", view.Rows[5].Connector.BuildConnector());
    }

    [Fact]
    public void Create_Should_OrderEverySiblingByPriorityThenId()
    {
        // arrange
        var model = Model(
            [
                Node("root-b", type: TaskTypes.Epic, priority: 1),
                Node("root-a", type: TaskTypes.Epic, priority: 1),
                Node("root-c", priority: 2),
                Node("child-z", priority: 2),
                Node("child-a", priority: 1),
                Node("child-b", priority: 1)
            ],
            [
                Edge("root-a", "child-z", GraphEdgeKind.ParentChild),
                Edge("root-a", "child-a", GraphEdgeKind.ParentChild),
                Edge("root-a", "child-b", GraphEdgeKind.ParentChild)
            ]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        Assert.Equal([null, "root-a", "child-a", "child-b", "child-z", "root-b", "root-c"], view.Rows.Select(t => t.TaskId));
    }

    [Fact]
    public void Create_Should_HideTerminalTasksAndTheirBlockingEdgesByDefault()
    {
        // arrange
        var model = Model(
            [Node("open"), Node("closed", status: TaskStates.Closed)],
            [Edge("open", "closed")]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        var open = Assert.Single(view.Rows, t => t.TaskId == "open");
        Assert.Equal([null, "open"], view.Rows.Select(t => t.TaskId));
        Assert.Equal((0, 0), (open.BlockedByCount, open.BlocksCount));
    }

    [Fact]
    public void Create_Should_CountVisibleBlockingEdgesAndHighlightSelectionRelationships()
    {
        // arrange
        var model = Model(
            [Node("blocker"), Node("selected"), Node("dependent")],
            [Edge("blocker", "selected"), Edge("selected", "dependent")]);
        var view = new GraphTreeView(model, Set());

        // act
        view.SelectTask("selected");

        // assert
        var blocker = Assert.Single(view.Rows, t => t.TaskId == "blocker");
        var selected = Assert.Single(view.Rows, t => t.TaskId == "selected");
        var dependent = Assert.Single(view.Rows, t => t.TaskId == "dependent");
        Assert.Equal((0, 1, true), (blocker.BlockedByCount, blocker.BlocksCount, blocker.IsRelatedToSelection));
        Assert.Equal((1, 1, true), (selected.BlockedByCount, selected.BlocksCount, selected.IsSelected));
        Assert.Equal((1, 0, true), (dependent.BlockedByCount, dependent.BlocksCount, dependent.IsRelatedToSelection));
    }

    [Fact]
    public void Create_Should_MarkCollapsedEpicWithContainedMatchesWithoutExpandingIt()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("first"), Node("second")],
            [
                Edge("epic", "first", GraphEdgeKind.ParentChild),
                Edge("epic", "second", GraphEdgeKind.ParentChild)
            ]);
        var view = new GraphTreeView(model, Set("epic"));

        // act
        view.SetMatchIds(["first", "second"]);
        view.SelectTask("first");

        // assert
        var epic = Assert.Single(view.Rows, t => t.TaskId == "epic");
        Assert.Equal([null, "epic"], view.Rows.Select(t => t.TaskId));
        Assert.False(epic.IsExpanded);
        Assert.Equal(2, epic.ContainedMatchCount);
        Assert.Equal(0, view.Rows.Count(t => t.IsSelected));
    }

    [Fact]
    public void Create_Should_ExpandAtTheAdaptiveThresholdAndCollapseAboveIt()
    {
        // arrange
        var atThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold - 1);
        var aboveThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold);

        // act
        var expanded = new GraphTreeView(atThreshold);
        var collapsed = new GraphTreeView(aboveThreshold);

        // assert
        Assert.Equal(GraphReductionOptions.AdaptiveCollapseThreshold + 1, expanded.Rows.Count);
        Assert.True(Assert.Single(expanded.Rows, t => t.TaskId == "epic").IsExpanded);
        Assert.Equal(2, collapsed.Rows.Count);
        Assert.False(Assert.Single(collapsed.Rows, t => t.TaskId == "epic").IsExpanded);
    }

    [Fact]
    public void CollapseSelectedAndExpandSelected_Should_ToggleTheSelectedEpic()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("child")],
            [Edge("epic", "child", GraphEdgeKind.ParentChild)]);
        var view = new GraphTreeView(model, Set());

        // act
        view.CollapseSelected();
        var collapsedRows = view.Rows;
        view.ExpandSelected();

        // assert
        Assert.Equal([null, "epic"], collapsedRows.Select(t => t.TaskId));
        Assert.False(Assert.Single(collapsedRows, t => t.TaskId == "epic").IsExpanded);
        Assert.Equal([null, "epic", "child"], view.Rows.Select(t => t.TaskId));
        Assert.True(Assert.Single(view.Rows, t => t.TaskId == "epic").IsExpanded);
    }

    [Fact]
    public void Render_Should_ShowTitlesBeforeDimmedIdsAndDependencyBadges()
    {
        // arrange
        var model = Model(
            [Node("epic", title: "Ship the graph", type: TaskTypes.Epic), Node("child")],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("epic", "child")
            ]);
        var view = new GraphTreeView(model, Set());
        var console = new TestConsole().Width(120).Height(10);

        // act
        console.Write(view.Render(120, 10));

        // assert
        Assert.Contains("▾ ○ [E] Ship the graph epic  blocked by 0 / blocks 1", console.Output);
        Assert.Contains("└─   ○ [T] child child  blocked by 1 / blocks 0", console.Output);
    }

    private static GraphModel EpicWithChildren(int childCount)
    {
        var nodes = new List<GraphNode> { Node("epic", type: TaskTypes.Epic) };
        var edges = new List<GraphEdge>();

        for (var index = 0; index < childCount; index++)
        {
            var childId = $"child-{index:D3}";
            nodes.Add(Node(childId));
            edges.Add(Edge("epic", childId, GraphEdgeKind.ParentChild));
        }

        return Model(nodes, edges);
    }

    private static GraphModel Model(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
        => new(nodes, edges);

    private static GraphNode Node(
        string id,
        string? title = null,
        string status = TaskStates.Open,
        string type = TaskTypes.Task,
        int priority = 2)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Status = status,
            Type = type,
            Priority = priority
        };

    private static GraphEdge Edge(
        string fromId,
        string toId,
        GraphEdgeKind kind = GraphEdgeKind.Blocks)
        => new(fromId, toId, kind);

    private static IReadOnlySet<string> Set(params string[] ids)
        => ids.ToHashSet(StringComparer.Ordinal);
}
