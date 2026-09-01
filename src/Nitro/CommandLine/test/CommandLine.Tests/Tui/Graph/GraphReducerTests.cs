using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphReducerTests
{
    [Fact]
    public void Reduce_Should_RemoveClosedNodesAndTheirEdges_When_HideClosedIsEnabled()
    {
        // arrange
        var model = Model(
            [Node("open"), Node("closed", status: TaskStates.Closed)],
            [Edge("open", "closed")]);

        // act
        var reduced = GraphReducer.Reduce(model);

        // assert
        Assert.Equal(["open"], reduced.Nodes.Select(t => t.Id));
        Assert.Empty(reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_RemoveNodesOutsideTheSelectedLabels_When_LabelsAreFiltered()
    {
        // arrange
        var model = Model(
            [Node("alpha", labels: ["one", "two"]), Node("beta", labels: ["one"]), Node("gamma", labels: ["two"])],
            [Edge("alpha", "beta"), Edge("alpha", "gamma")]);
        var options = new GraphReductionOptions { Labels = Set("one", "two") };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        Assert.Equal(["alpha"], reduced.Nodes.Select(t => t.Id));
        Assert.Empty(reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_KeepAnEpicAndItsDescendants_When_EpicIsFiltered()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("child"), Node("outside")],
            [Edge("epic", "child", GraphEdgeKind.ParentChild)]);
        var options = new GraphReductionOptions { EpicIds = Set("epic") };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        Assert.Equal(["child", "epic"], reduced.Nodes.Select(t => t.Id));
        Assert.Equal([Edge("epic", "child", GraphEdgeKind.ParentChild)], reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_ApplyLabelsAfterSelectingEpicDescendants()
    {
        // arrange
        var model = Model(
            [
                Node("epic", type: TaskTypes.Epic),
                Node("child", labels: ["alpha", "beta"]),
                Node("sibling", labels: ["alpha"]),
                Node("outside", labels: ["alpha", "beta"])
            ],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("epic", "sibling", GraphEdgeKind.ParentChild)
            ]);

        // act
        var reduced = GraphReducer.Reduce(
            model,
            new GraphReductionOptions { EpicIds = Set("epic"), Labels = Set("alpha", "beta") });

        // assert
        Assert.Equal(["child"], reduced.Nodes.Select(t => t.Id));
        Assert.Empty(reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_KeepOpenDescendantsBehindClosedIntermediates_When_HidingClosedTasks()
    {
        // arrange
        var model = Model(
            [
                Node("epic", type: TaskTypes.Epic),
                Node("middle", status: TaskStates.Closed),
                Node("descendant")
            ],
            [
                Edge("epic", "middle", GraphEdgeKind.ParentChild),
                Edge("middle", "descendant", GraphEdgeKind.ParentChild)
            ]);

        // act
        var reduced = GraphReducer.Reduce(model, new GraphReductionOptions { EpicIds = Set("epic") });

        // assert
        Assert.Equal(["descendant", "epic"], reduced.Nodes.Select(t => t.Id));
        Assert.Empty(reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_CollapseEpicChildrenAndReattachCrossBoundaryEdges_When_EpicIsCollapsed()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("child"), Node("outside")],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("child", "outside"),
                Edge("outside", "child")
            ]);
        var options = new GraphReductionOptions { CollapsedEpicIds = Set("epic") };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        var epic = Assert.Single(reduced.Nodes, t => t.Id == "epic");
        Assert.Equal(1, epic.HiddenChildCount);
        Assert.Equal(
            [Edge("epic", "outside"), Edge("outside", "epic", isReversed: true)],
            reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_CollapseNestedDescendantsIntoTheOuterEpic_When_OnlyTheOuterEpicIsCollapsed()
    {
        // arrange
        var model = Model(
            [
                Node("outer", type: TaskTypes.Epic),
                Node("inner", type: TaskTypes.Epic),
                Node("descendant"),
                Node("outside")
            ],
            [
                Edge("outer", "inner", GraphEdgeKind.ParentChild),
                Edge("inner", "descendant", GraphEdgeKind.ParentChild),
                Edge("descendant", "outside"),
                Edge("outside", "descendant")
            ]);
        var options = new GraphReductionOptions { CollapsedEpicIds = Set("outer") };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        var outer = Assert.Single(reduced.Nodes, t => t.Id == "outer");
        Assert.Equal(["outer", "outside"], reduced.Nodes.Select(t => t.Id));
        Assert.Equal(2, outer.HiddenChildCount);
        Assert.Equal(
            [Edge("outer", "outside"), Edge("outside", "outer", isReversed: true)],
            reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_UseTheCanonicalPriorityParent_When_AChildHasMultipleParents()
    {
        // arrange
        var model = Model(
            [
                Node("z", type: TaskTypes.Epic, priority: 0),
                Node("a", type: TaskTypes.Epic, priority: 4),
                Node("child", priority: 2)
            ],
            [
                Edge("a", "child", GraphEdgeKind.ParentChild),
                Edge("z", "child", GraphEdgeKind.ParentChild)
            ]);

        // act
        var reduced = GraphReducer.Reduce(
            model,
            new GraphReductionOptions { HideClosed = false, CollapsedEpicIds = Set("z") });

        // assert
        Assert.Equal(["z", "a"], reduced.Nodes.Select(t => t.Id));
        Assert.Equal([Edge("a", "z", GraphEdgeKind.ParentChild)], reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_ApplyEveryStage_When_OptionsAreComposed()
    {
        // arrange
        var model = Model(
            [
                Node("epic", type: TaskTypes.Epic, labels: ["work"]),
                Node("child", labels: ["work"]),
                Node("closed", status: TaskStates.Closed, labels: ["work"]),
                Node("outside", labels: ["other"])
            ],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("epic", "closed", GraphEdgeKind.ParentChild),
                Edge("child", "outside")
            ]);
        var options = new GraphReductionOptions
        {
            Labels = Set("work"),
            EpicIds = Set("epic"),
            CollapsedEpicIds = Set("epic")
        };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        var node = Assert.Single(reduced.Nodes);
        Assert.Equal("epic", node.Id);
        Assert.Equal(1, node.HiddenChildCount);
        Assert.Empty(reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_CollapseEpicsOnlyAboveTheAdaptiveThreshold_When_CollapseStateIsUnspecified()
    {
        // arrange
        var atThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold - 1);
        var aboveThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold);

        // act
        var thresholdResult = GraphReducer.Reduce(atThreshold);
        var aboveThresholdResult = GraphReducer.Reduce(aboveThreshold);

        // assert
        Assert.Equal(GraphReductionOptions.AdaptiveCollapseThreshold, thresholdResult.Nodes.Count);
        Assert.Single(aboveThresholdResult.Nodes);
    }

    [Fact]
    public void Reduce_Should_ForceClosedHidingAndEpicCollapse_When_VisibleNodeCapIsExceeded()
    {
        // arrange
        var model = EpicWithChildren(GraphReductionOptions.VisibleNodeCap, includeClosedChild: true);
        var options = new GraphReductionOptions
        {
            HideClosed = false,
            CollapsedEpicIds = Set()
        };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        Assert.True(reduced.IsReduced);
        Assert.Single(reduced.Nodes);
        Assert.Equal(GraphReductionOptions.VisibleNodeCap + 1, reduced.HiddenNodeCount);
    }

    [Fact]
    public void Reduce_Should_MarkTheLayoutBackEdge_When_CollapsedEpicsFormATwoCycle()
    {
        // arrange
        var model = Model(
            [
                Node("first", type: TaskTypes.Epic),
                Node("first-child"),
                Node("second", type: TaskTypes.Epic),
                Node("second-child")
            ],
            [
                Edge("first", "first-child", GraphEdgeKind.ParentChild),
                Edge("second", "second-child", GraphEdgeKind.ParentChild),
                Edge("first-child", "second-child"),
                Edge("second-child", "first-child")
            ]);
        var options = new GraphReductionOptions { CollapsedEpicIds = Set("first", "second") };

        // act
        var reduced = GraphReducer.Reduce(model, options);

        // assert
        Assert.Equal(
            [Edge("first", "second"), Edge("second", "first", isReversed: true)],
            reduced.Edges);
    }

    [Fact]
    public void Reduce_Should_OrderNodesAndEdgesByPriorityThenId_When_InputOrderDiffers()
    {
        // arrange
        var model = Model(
            [Node("z", priority: 2), Node("b", priority: 1), Node("a", priority: 1)],
            [Edge("z", "a"), Edge("b", "z"), Edge("a", "z")]);

        // act
        var reduced = GraphReducer.Reduce(
            model,
            new GraphReductionOptions { HideClosed = false, CollapsedEpicIds = Set() });

        // assert
        Assert.Equal(["a", "b", "z"], reduced.Nodes.Select(t => t.Id));
        Assert.Equal([Edge("a", "z"), Edge("b", "z"), Edge("z", "a", isReversed: true)], reduced.Edges);
    }

    private static GraphModel EpicWithChildren(int childCount, bool includeClosedChild = false)
    {
        var nodes = new List<GraphNode> { Node("epic", type: TaskTypes.Epic) };
        var edges = new List<GraphEdge>();

        for (var i = 0; i < childCount; i++)
        {
            var id = $"child-{i:D3}";
            nodes.Add(Node(id));
            edges.Add(Edge("epic", id, GraphEdgeKind.ParentChild));
        }

        if (includeClosedChild)
        {
            nodes.Add(Node("closed", status: TaskStates.Closed));
            edges.Add(Edge("epic", "closed", GraphEdgeKind.ParentChild));
        }

        return Model(nodes, edges);
    }

    private static GraphModel Model(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
        => new(nodes, edges);

    private static GraphNode Node(
        string id,
        string status = TaskStates.Open,
        string type = TaskTypes.Task,
        int priority = 2,
        IReadOnlyList<string>? labels = null)
        => new()
        {
            Id = id,
            Title = id,
            Status = status,
            Type = type,
            Priority = priority,
            Labels = labels ?? []
        };

    private static GraphEdge Edge(
        string fromId,
        string toId,
        GraphEdgeKind kind = GraphEdgeKind.Blocks,
        bool isReversed = false)
        => new(fromId, toId, kind, isReversed);

    private static IReadOnlySet<string> Set(params string[] values)
        => values.ToHashSet(StringComparer.Ordinal);
}
