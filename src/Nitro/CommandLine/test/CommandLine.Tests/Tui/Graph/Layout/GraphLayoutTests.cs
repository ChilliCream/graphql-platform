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
    public void Layout_Should_PreserveValidSeededLayers_When_AnEdgeIsReplaced()
    {
        // arrange
        var initial = Model(
            [Node("a"), Node("b"), Node("c")],
            [Edge("a", "b"), Edge("b", "c")]);
        var mutated = Model(
            [Node("a"), Node("c")],
            [Edge("a", "c")]);
        var layout = new GraphLayout();
        var initialFrame = layout.Layout(initial, Sizes(initial));

        // act
        var unseededFrame = layout.Layout(mutated, Sizes(mutated));
        var seededFrame = layout.Layout(mutated, Sizes(mutated), initialFrame);

        // assert
        Assert.Equal(1, unseededFrame.FindNode("c")!.Layer);
        Assert.Equal(2, seededFrame.FindNode("c")!.Layer);
        Assert.Equal(0, seededFrame.FindNode("a")!.Order);
        Assert.Equal(0, seededFrame.FindNode("c")!.Order);
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
    public void Layout_Should_MatchFullCrossingRecount_When_TransposingLayers()
    {
        // arrange
        var model = Model(
            [Node("a"), Node("b"), Node("c"), Node("d"), Node("e"), Node("f")],
            [Edge("a", "d"), Edge("a", "e"), Edge("b", "c"), Edge("b", "f"), Edge("c", "e"), Edge("d", "f")]);

        // act
        var result = new GraphLayout().Layout(model, Sizes(model));

        // assert
        Assert.Equal(CountCrossings(result.EdgeSpans), result.CrossingCount);
    }

    [Fact]
    public void Layout_Should_AcceptTransposeCandidatesOnlyWhenTheirFullCrossingCountImproves()
    {
        // arrange
        var model = Model(
            [Node("a"), Node("b"), Node("c"), Node("d"), Node("e"), Node("f")],
            [Edge("a", "d"), Edge("a", "e"), Edge("b", "c"), Edge("b", "f"), Edge("c", "e"), Edge("d", "f")]);
        var metrics = new GraphLayoutMetrics(captureCandidateObservations: true);

        // act
        _ = new GraphLayout(metrics: metrics).Layout(model, Sizes(model));

        // assert
        Assert.Contains(metrics.CandidateObservations, t => t.Accepted);
        Assert.Contains(metrics.CandidateObservations, t => !t.Accepted);
        Assert.All(
            metrics.CandidateObservations,
            t =>
            {
                Assert.Equal(t.FullAfter - t.FullBefore, t.IncidentAfter - t.IncidentBefore);
                Assert.Equal(t.FullAfter < t.FullBefore, t.Accepted);
            });
    }

    [Fact]
    public void Layout_Should_UseIncidentComparisons_ForRepresentativeLargeGraphs()
    {
        // arrange
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        for (var layer = 0; layer < 2; layer++)
        {
            for (var index = 0; index < 200; index++)
            {
                nodes.Add(Node($"{layer:D2}-{index:D3}"));
            }
        }

        for (var index = 0; index < 200; index++)
        {
            edges.Add(Edge($"00-{index:D3}", $"01-{index:D3}"));
        }

        for (var index = 0; index < 200; index++)
        {
            edges.Add(Edge($"00-{index:D3}", $"01-{(index + 1) % 200:D3}"));
        }

        var model = Model(nodes, edges);
        var metrics = new GraphLayoutMetrics();

        // act
        var result = new GraphLayout(metrics: metrics).Layout(model, Sizes(model));

        // assert
        var fullRecountComparisons = result.EdgeSpans
            .GroupBy(t => t.FromLayer)
            .Sum(t => (long)t.Count() * (t.Count() - 1) / 2);
        var legacyCandidateWork = metrics.CandidateCount * fullRecountComparisons * 2;
        Assert.Equal(400, result.EdgeSpans.Count);
        Assert.True(legacyCandidateWork > 500_000_000);
        Assert.True(metrics.IncidentComparisonCount * 40L < legacyCandidateWork);
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

    private static int CountCrossings(IReadOnlyList<GraphLayoutEdgeSpan> spans)
    {
        var count = 0;
        foreach (var layer in spans.GroupBy(t => t.FromLayer))
        {
            var ordered = layer.ToArray();
            for (var left = 0; left < ordered.Length; left++)
            {
                for (var right = left + 1; right < ordered.Length; right++)
                {
                    var from = ordered[left].FromOrder.CompareTo(ordered[right].FromOrder);
                    var to = ordered[left].ToOrder.CompareTo(ordered[right].ToOrder);
                    if (from != 0 && to != 0 && from != to)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }
}
