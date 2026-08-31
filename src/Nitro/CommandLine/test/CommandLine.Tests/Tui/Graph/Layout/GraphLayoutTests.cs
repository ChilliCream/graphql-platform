using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.Layout;

public sealed class GraphLayoutTests
{
    [Fact]
    public void Layout_Should_ProduceTheSameFrame_When_InputOrderingDiffers()
    {
        // arrange
        var first = Model(
            [Node("a"), Node("b"), Node("c"), Node("d")],
            [Edge("a", "c"), Edge("a", "d"), Edge("b", "c"), Edge("b", "d")]);
        var second = Model(
            [Node("d"), Node("b"), Node("c"), Node("a")],
            [Edge("b", "d"), Edge("a", "d"), Edge("b", "c"), Edge("a", "c")]);
        var layout = new GraphLayout();

        // act
        var firstFrame = layout.Layout(first, Sizes(first));
        var secondFrame = layout.Layout(second, Sizes(second));

        // assert
        Assert.Equal(firstFrame.Nodes, secondFrame.Nodes);
        Assert.Equal(firstFrame.EdgeSpans, secondFrame.EdgeSpans);
        Assert.Equal(firstFrame.CrossingCount, secondFrame.CrossingCount);
    }

    [Fact]
    public void Layout_Should_PreserveExistingOrders_When_SeededAfterAnUnrelatedMutation()
    {
        // arrange
        var initial = Model(
            [Node("a"), Node("b"), Node("c"), Node("d")],
            [Edge("a", "c"), Edge("b", "d")]);
        var mutated = Model(
            [Node("x"), Node("d"), Node("b"), Node("a"), Node("c")],
            [Edge("b", "d"), Edge("a", "c")]);
        var layout = new GraphLayout();
        var firstFrame = layout.Layout(initial, Sizes(initial));

        // act
        var seededFrame = layout.Layout(mutated, Sizes(mutated), firstFrame);

        // assert
        Assert.Equal(
            firstFrame.Nodes.Select(t => (t.Id, t.Layer, t.Order)),
            seededFrame.Nodes.Where(t => t.Id != "x").Select(t => (t.Id, t.Layer, t.Order)));
    }

    [Fact]
    public void Layout_Should_ReportTheKnownBicliqueCrossingCount()
    {
        // arrange
        var model = Model(
            [Node("a"), Node("b"), Node("c"), Node("d")],
            [Edge("a", "c"), Edge("a", "d"), Edge("b", "c"), Edge("b", "d")]);

        // act
        var result = new GraphLayout().Layout(model, Sizes(model));

        // assert
        Assert.Equal(1, result.CrossingCount);
    }

    [Fact]
    public void Layout_Should_PlaceEveryForwardEdgeInIncreasingLeftToRightLayers()
    {
        // arrange
        var model = Model(
            [Node("a"), Node("b"), Node("c"), Node("d")],
            [Edge("a", "b"), Edge("a", "c"), Edge("b", "d"), Edge("c", "d")]);

        // act
        var result = new GraphLayout().Layout(model, Sizes(model));

        // assert
        Assert.All(result.EdgeSpans, edge => Assert.Equal(edge.FromLayer + 1, edge.ToLayer));
        Assert.All(model.Edges, edge => Assert.True(result.FindNode(edge.FromId)!.Layer < result.FindNode(edge.ToId)!.Layer));
    }

    [Fact]
    public void Layout_Should_ReverseOneDeterministicBackEdge_When_TheModelContainsACycle()
    {
        // arrange
        var model = Model(
            [Node("a"), Node("b"), Node("c")],
            [Edge("a", "b"), Edge("b", "c"), Edge("c", "a")]);

        // act
        var result = new GraphLayout().Layout(model, Sizes(model));

        // assert
        Assert.Equal(1, result.ReversedEdgeCount);
        Assert.All(result.EdgeSpans, edge => Assert.Equal(edge.FromLayer + 1, edge.ToLayer));
    }

    [Fact]
    public void Layout_Should_UseMeasuredNodeDimensionsForCellPositions()
    {
        // arrange
        var model = Model([Node("a"), Node("b")], [Edge("a", "b")]);
        var sizes = new Dictionary<string, GraphNodeSize>(StringComparer.Ordinal)
        {
            ["a"] = new(7, 3),
            ["b"] = new(5, 2)
        };

        // act
        var result = new GraphLayout().Layout(model, sizes, layerSpacing: 2);

        // assert
        Assert.Equal(new GraphNodeSize(7, 3), new(result.FindNode("a")!.Width, result.FindNode("a")!.Height));
        Assert.Equal(9, result.FindNode("b")!.X);
    }

    private static GraphModel Model(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
        => new(nodes, edges);

    private static GraphNode Node(string id)
        => new()
        {
            Id = id,
            Title = id,
            Status = "open",
            Type = "task",
            Priority = 0
        };

    private static GraphEdge Edge(string fromId, string toId)
        => new(fromId, toId, GraphEdgeKind.Blocks);

    private static IReadOnlyDictionary<string, GraphNodeSize> Sizes(GraphModel model)
        => model.Nodes.ToDictionary(t => t.Id, _ => new GraphNodeSize(1, 1), StringComparer.Ordinal);
}
