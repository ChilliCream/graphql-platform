using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.Render;

public sealed class GraphEdgeRouterTests
{
    [Fact]
    public void Route_Should_KeepEveryDenseFixtureEdgeOutsideNodeBoxes()
    {
        // arrange
        var layout = Frame(
            [
                Node("a", 0, 0, 3, 2, 0, 0), Node("b", 0, 4, 3, 2, 0, 1),
                Node("c", 10, 0, 3, 2, 1, 0), Node("d", 10, 4, 3, 2, 1, 1)
            ],
            [
                Span("a", "c", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(10, 0)),
                Span("a", "d", 0, 1, 0, 1, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(10, 4)),
                Span("b", "c", 0, 1, 1, 0, new GraphLayoutPoint(0, 4), new GraphLayoutPoint(10, 0)),
                Span("b", "d", 0, 1, 1, 1, new GraphLayoutPoint(0, 4), new GraphLayoutPoint(10, 4))
            ]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var intrusions = result.Routes
            .SelectMany(t => t.Points)
            .Where(point => layout.Nodes.Any(node => Contains(node, point)))
            .ToArray();

        // assert
        Assert.Equal([], intrusions);
    }

    [Fact]
    public void Connect_Should_UseBoxDrawingGlyphs_When_DirectionsJoin()
    {
        // arrange
        var buffer = new CellBuffer(6, 1);
        var owner = new object();

        // act
        buffer.Connect(0, 0, CanvasDirections.Left | CanvasDirections.Right, Style.Plain, owner);
        buffer.Connect(1, 0, CanvasDirections.Up | CanvasDirections.Down, Style.Plain, owner);
        buffer.Connect(2, 0, CanvasDirections.Right | CanvasDirections.Down, Style.Plain, owner);
        buffer.Connect(3, 0, CanvasDirections.Left | CanvasDirections.Up, Style.Plain, owner);
        buffer.Connect(4, 0, CanvasDirections.Up | CanvasDirections.Right | CanvasDirections.Down, Style.Plain, owner);
        buffer.Connect(5, 0, CanvasDirections.Up | CanvasDirections.Right | CanvasDirections.Down | CanvasDirections.Left, Style.Plain, owner);

        // assert
        buffer.ToText(new CanvasViewport(0, 0, 6, 1)).MatchInlineSnapshot("─│┌┘├┼");
    }

    [Fact]
    public void Route_Should_DashAndPointLeft_When_LayoutArcIsReversed()
    {
        // arrange
        var layout = Frame(
            [Node("dependency", 0, 0, 2, 1, 0, 0), Node("dependent", 8, 2, 2, 1, 1, 0)],
            [Span("dependent", "dependency", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 2), reversed: true)]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var route = Assert.Single(result.Routes);

        // assert
        Assert.Equal('◀', result.Buffer.Get(2, 0).Glyph);
        Assert.Contains(route.Points, point => result.Buffer.Get(point.X, point.Y).Glyph is '┄' or '┆');
        Assert.All(
            route.Points,
            point => Assert.True((result.Buffer.Get(point.X, point.Y).Style.Decoration & Decoration.Dim) != 0));
    }

    [Fact]
    public void Route_Should_ApplyOverrideOnlyToCellsOwnedByTargetedEdge()
    {
        // arrange
        var first = Edge("a", "c");
        var second = Edge("b", "d");
        var layout = Frame(
            [
                Node("a", 0, 0, 2, 1, 0, 0), Node("b", 0, 3, 2, 1, 0, 1),
                Node("c", 8, 0, 2, 1, 1, 0), Node("d", 8, 3, 2, 1, 1, 1)
            ],
            [
                Span(first, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0)),
                Span(second, 0, 1, 1, 1, new GraphLayoutPoint(0, 3), new GraphLayoutPoint(8, 3))
            ]);
        var normal = new GraphEdgeRouter().Route(layout);

        // act
        var highlighted = new GraphEdgeRouter().Route(
            layout,
            new GraphEdgeRenderOptions { StyleOverride = edge => edge == first ? new Style(Color.Red) : (Style?)null });
        var changed = Cells(highlighted.Buffer)
            .Where(point => normal.Buffer.Get(point.X, point.Y).Style != highlighted.Buffer.Get(point.X, point.Y).Style)
            .ToArray();

        // assert
        Assert.NotEmpty(changed);
        Assert.All(changed, point => Assert.Contains((object)first, highlighted.Buffer.Get(point.X, point.Y).Owners));
    }

    [Fact]
    public void Route_Should_UseFilledAndHollowArrowheads_When_EdgeKindsDiffer()
    {
        // arrange
        var layout = Frame(
            [Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 0, 2, 1, 1, 0), Node("c", 8, 3, 2, 1, 1, 1)],
            [
                Span("a", "b", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0)),
                Span("a", "c", 0, 1, 0, 1, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 3), kind: GraphEdgeKind.ParentChild)
            ]);

        // act
        var result = new GraphEdgeRouter().Route(layout, new GraphEdgeRenderOptions { IncludeParentChild = true });
        var arrows = result.Routes.Select(route => result.Buffer.Get(route.Points[^1].X, route.Points[^1].Y).Glyph);

        // assert
        Assert.Equal(['▶', '▷'], arrows);
    }

    [Fact]
    public void Route_Should_ReserveDistinctFanInArrowPorts_When_InputOrderChanges()
    {
        // arrange
        var blocks = Edge("a", "target");
        var parent = Edge("c", "target", GraphEdgeKind.ParentChild);
        var nodes = new[]
        {
            Node("a", 0, 0, 2, 1, 0, 0), Node("c", 0, 3, 2, 1, 0, 1),
            Node("target", 8, 0, 2, 4, 1, 0)
        };
        var spans = new[]
        {
            Span(blocks, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0)),
            Span(parent, 0, 1, 1, 0, new GraphLayoutPoint(0, 3), new GraphLayoutPoint(8, 0))
        };
        var options = new GraphEdgeRenderOptions { IncludeParentChild = true };
        var first = new GraphEdgeRouter().Route(Frame(nodes, spans), options);

        // act
        var second = new GraphEdgeRouter().Route(Frame(nodes, spans.Reverse().ToArray()), options);
        var arrows = ArrowCells(first.Buffer).OrderBy(t => t.Point.Y).ThenBy(t => t.Point.X).ToArray();

        // assert
        Assert.Equal(2, first.RenderedEdgeCount);
        Assert.Equal(['▶', '▷'], arrows.Select(t => t.Glyph).Order().ToArray());
        Assert.Equal(2, arrows.Select(t => t.Point).Distinct().Count());
        Assert.All(first.Routes, route => Assert.Equal(1, route.Points.Count(point => arrows.Any(arrow => arrow.Point == point))));
        Assert.Equal(RouteProjection(first), RouteProjection(second));
    }

    [Fact]
    public void Route_Should_UseOneArrowForEveryLongLogicalEdge()
    {
        // arrange
        var edge = Edge("a", "b");
        var layout = Frame(
            [Node("a", 0, 0, 2, 1, 0, 0), Node("b", 15, 0, 2, 1, 3, 0)],
            [
                Span(edge, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(5, 0)),
                Span(edge, 1, 2, 0, 0, new GraphLayoutPoint(5, 0), new GraphLayoutPoint(10, 0)),
                Span(edge, 2, 3, 0, 0, new GraphLayoutPoint(10, 0), new GraphLayoutPoint(15, 0))
            ]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var arrows = Cells(result.Buffer)
            .Where(point => result.Buffer.Get(point.X, point.Y).Glyph is '▶' or '▷' or '◀' or '◁')
            .ToArray();

        // assert
        Assert.Equal(3, result.Routes.Count);
        Assert.Single(arrows);
        Assert.Equal(new GraphLayoutPoint(14, 0), arrows[0]);
        Assert.Equal('─', result.Buffer.Get(5, 0).Glyph);
        Assert.Equal("nodes: 2  edges: 1  grid: 17 x 1  crossings: 0  reversed: 0", GraphRenderFooter.CreateText(result));
    }

    [Fact]
    public void Route_Should_FindDeterministicFallbackOutsideEveryNode()
    {
        // arrange
        var layout = Frame(
            [
                Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 4, 2, 1, 1, 0),
                Node("obstacle", 2, 1, 6, 3, 2, 0)
            ],
            [Span("a", "b", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 4))]);

        // act
        var first = new GraphEdgeRouter().Route(layout).Routes[0].Points;
        var second = new GraphEdgeRouter().Route(layout).Routes[0].Points;

        // assert
        Assert.Equal(first, second);
        Assert.Equal([], first.Where(point => layout.Nodes.Any(node => Contains(node, point))).ToArray());
        Assert.All(first.Zip(first.Skip(1)), pair => Assert.Equal(1, Math.Abs(pair.First.X - pair.Second.X) + Math.Abs(pair.First.Y - pair.Second.Y)));
    }

    [Fact]
    public void Route_Should_PlaceReversedLongEdgeArrowAtSemanticTarget()
    {
        // arrange
        var edge = Edge("a", "b");
        var layout = Frame(
            [Node("b", 0, 0, 2, 1, 0, 0), Node("a", 15, 0, 2, 1, 3, 0)],
            [
                Span(edge, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(5, 0), reversed: true),
                Span(edge, 1, 2, 0, 0, new GraphLayoutPoint(5, 0), new GraphLayoutPoint(10, 0), reversed: true),
                Span(edge, 2, 3, 0, 0, new GraphLayoutPoint(10, 0), new GraphLayoutPoint(15, 0), reversed: true)
            ]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var route = result.Routes[0];
        var arrows = Cells(result.Buffer)
            .Where(point => result.Buffer.Get(point.X, point.Y).Glyph is '▶' or '▷' or '◀' or '◁')
            .ToArray();

        // assert
        Assert.Single(arrows);
        Assert.Equal(new GraphLayoutPoint(2, 0), arrows[0]);
        Assert.Equal('◀', result.Buffer.Get(arrows[0].X, arrows[0].Y).Glyph);
        Assert.Equal('┄', result.Buffer.Get(5, 0).Glyph);
        Assert.Equal([new GraphLayoutPoint(2, 0), new GraphLayoutPoint(3, 0)], route.Points.Take(2));
    }

    [Fact]
    public void Route_Should_RejectEndpointPortAliases_When_CompactReversedLayoutUsesOpposingAnchors()
    {
        // arrange
        var edge = Edge("other", "target");
        var overrideStyle = new Style(Color.Red);
        var layout = Frame(
            [Node("target", 0, 0, 2, 1, 0, 0), Node("other", 4, 0, 2, 1, 1, 0)],
            [Span(edge, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(4, 0), reversed: true)]);

        // act
        var result = new GraphEdgeRouter().Route(
            layout,
            new GraphEdgeRenderOptions { StyleOverride = current => current == edge ? overrideStyle : (Style?)null });
        var route = result.Routes.Single();
        var arrow = ArrowCells(result.Buffer).Single();
        var startPort = route.Points[0];
        var endPort = route.Points[^1];
        var snapshot = $"""
            endpoint ports unique: {route.Points.Count(point => point == startPort) == 1 && route.Points.Count(point => point == endPort) == 1}
            outward anchors: {route.Points[1] == new GraphLayoutPoint(startPort.X + 1, startPort.Y) && route.Points[^2] == new GraphLayoutPoint(endPort.X + 1, endPort.Y)}
            manhattan adjacent: {route.Points.Zip(route.Points.Skip(1)).All(pair => Math.Abs(pair.First.X - pair.Second.X) + Math.Abs(pair.First.Y - pair.Second.Y) == 1)}
            outside nodes: {route.Points.All(point => layout.Nodes.All(node => !Contains(node, point)))}
            true target arrow: {arrow.Point == new GraphLayoutPoint(2, 0) && arrow.Glyph == '◀'}
            reversed override: {result.Buffer.Get(arrow.Point.X, arrow.Point.Y).Style == new Style(Color.Red, null, Decoration.Dim)}
            """;

        // assert
        snapshot.MatchInlineSnapshot(
            """
            endpoint ports unique: True
            outward anchors: True
            manhattan adjacent: True
            outside nodes: True
            true target arrow: True
            reversed override: True
            """);
    }

    [Fact]
    public void Route_Should_DeduplicateEqualSpansForOneSemanticEdge()
    {
        // arrange
        var edge = Edge("a", "b");
        var span = Span(edge, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0));
        var layout = Frame(
            [Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 0, 2, 1, 1, 0)],
            [span, span]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var arrows = Cells(result.Buffer)
            .Where(point => result.Buffer.Get(point.X, point.Y).Glyph is '▶' or '▷' or '◀' or '◁' or '▼' or '▽' or '▲' or '△')
            .ToArray();

        // assert
        Assert.Single(result.Routes);
        Assert.Equal(1, result.RenderedEdgeCount);
        Assert.Single(arrows);
        Assert.Equal("nodes: 2  edges: 1  grid: 10 x 1  crossings: 0  reversed: 0", GraphRenderFooter.CreateText(result));
    }

    [Fact]
    public void Route_Should_UseFreePerimeterPortsWhenAlignedNodesTouch()
    {
        // arrange
        var layout = Frame(
            [
                Node("a", 0, 0, 2, 2, 0, 0), Node("b", 2, 0, 2, 2, 1, 0),
                Node("blocker", 4, 0, 1, 2, 2, 0)
            ],
            [Span("a", "b", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(2, 0))]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var route = Assert.Single(result.Routes);
        var intrusions = route.Points.Where(point => layout.Nodes.Any(node => Contains(node, point))).ToArray();

        // assert
        Assert.Equal(
            [
                new GraphLayoutPoint(0, 2), new GraphLayoutPoint(0, 3), new GraphLayoutPoint(1, 3),
                new GraphLayoutPoint(2, 3), new GraphLayoutPoint(2, 2)
            ],
            route.Points);
        Assert.Equal([], intrusions);
        Assert.Equal('▲', result.Buffer.Get(2, 2).Glyph);
        Assert.Equal(1, result.RenderedEdgeCount);
    }

    [Fact]
    public void Route_Should_DiscardSemanticEdgeWhenAnEndpointHasNoFreePort()
    {
        // arrange
        var layout = Frame(
            [
                Node("a", 1, 1, 1, 1, 0, 0), Node("left", 0, 1, 1, 1, 0, 1),
                Node("right", 2, 1, 1, 1, 0, 2), Node("top", 1, 0, 1, 1, 0, 3),
                Node("bottom", 1, 2, 1, 1, 0, 4), Node("b", 6, 1, 1, 1, 1, 0)
            ],
            [Span("a", "b", 0, 1, 0, 0, new GraphLayoutPoint(1, 1), new GraphLayoutPoint(6, 1))]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var arrows = Cells(result.Buffer)
            .Where(point => result.Buffer.Get(point.X, point.Y).Glyph is '▶' or '▷' or '◀' or '◁' or '▼' or '▽' or '▲' or '△')
            .ToArray();

        // assert
        Assert.Equal([], result.Routes);
        Assert.Equal(0, result.RenderedEdgeCount);
        Assert.Equal([], arrows);
    }

    [Fact]
    public void Route_Should_RollBackTargetReservation_When_AfterTargetSpanFails()
    {
        // arrange
        var failed = Edge("a", "b");
        var succeeding = Edge("e", "f");
        var nodes = new[]
        {
            Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 0, 2, 1, 1, 0),
            Node("e", 0, 6, 2, 1, 0, 1), Node("f", 8, 6, 2, 1, 1, 1),
            Node("left", 3, 4, 1, 1, 2, 0), Node("right", 5, 4, 1, 1, 2, 1),
            Node("top", 4, 3, 1, 1, 2, 2), Node("bottom", 4, 5, 1, 1, 2, 3)
        };
        var goodSpan = Span(succeeding, 0, 1, 1, 1, new GraphLayoutPoint(0, 6), new GraphLayoutPoint(8, 6));
        var failedSpans = new[]
        {
            Span(failed, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0)),
            Span(failed, 1, 2, 0, 0, new GraphLayoutPoint(4, 4), new GraphLayoutPoint(6, 4))
        };
        var withoutFailed = new GraphEdgeRouter().Route(Frame(nodes, [goodSpan]));

        // act
        var withFailed = new GraphEdgeRouter().Route(Frame(nodes, [.. failedSpans, goodSpan]));

        // assert
        Assert.Equal(1, withFailed.RenderedEdgeCount);
        Assert.Equal(RouteProjection(withoutFailed), RouteProjection(withFailed));
        Assert.Equal(ArrowCells(withoutFailed.Buffer), ArrowCells(withFailed.Buffer));
    }

    [Fact]
    public void Route_Should_KeepSharedCellContributionsStableAcrossInputOrder()
    {
        // arrange
        var highlighted = Edge("a", "b");
        var parent = Edge("a", "b", GraphEdgeKind.ParentChild);
        var highlightedSpan = Span(highlighted, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0), reversed: true);
        var parentSpan = Span(parent, 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0));
        var nodes = new[] { Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 0, 2, 3, 1, 0) };
        var options = new GraphEdgeRenderOptions
        {
            IncludeParentChild = true,
            BlocksStyle = new Style(Color.Blue),
            ParentChildStyle = new Style(Color.Green),
            StyleOverride = edge => edge == highlighted ? new Style(Color.Red) : (Style?)null
        };
        var first = new GraphEdgeRouter().Route(Frame(nodes, [highlightedSpan, parentSpan]), options);

        // act
        var second = new GraphEdgeRouter().Route(Frame(nodes, [parentSpan, highlightedSpan]), options);
        var point = new GraphLayoutPoint(3, 0);
        var firstCell = first.Buffer.Get(point.X, point.Y);
        var secondCell = second.Buffer.Get(point.X, point.Y);

        // assert
        Assert.Equal(RouteProjection(first), RouteProjection(second));
        Assert.Equal(2, firstCell.Owners.Count);
        Assert.Equal('┬', firstCell.Glyph);
        Assert.Equal(new Style(Color.Red, null, Decoration.Dim), firstCell.Style);
        Assert.Equal(Describe(firstCell), Describe(secondCell));
    }

    [Fact]
    public void CreateText_Should_ReportGraphCanvasMetrics()
    {
        // arrange
        var layout = Frame(
            [Node("a", 0, 0, 2, 1, 0, 0), Node("b", 8, 0, 2, 1, 1, 0)],
            [Span("a", "b", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 0))],
            crossings: 3,
            reversedCount: 1);
        var result = new GraphEdgeRouter().Route(layout);

        // act
        var footer = GraphRenderFooter.CreateText(result);

        // assert
        footer.MatchInlineSnapshot("nodes: 2  edges: 1  grid: 10 x 1  crossings: 3  reversed: 1");
    }

    private static bool Contains(GraphLayoutNode node, GraphLayoutPoint point)
        => point.X >= node.X
            && point.X < node.X + node.Width
            && point.Y >= node.Y
            && point.Y < node.Y + node.Height;

    private static IEnumerable<GraphLayoutPoint> Cells(CellBuffer buffer)
    {
        for (var y = 0; y < buffer.Height; y++)
        {
            for (var x = 0; x < buffer.Width; x++)
            {
                yield return new GraphLayoutPoint(x, y);
            }
        }
    }

    private static IEnumerable<ArrowCell> ArrowCells(CellBuffer buffer)
        => Cells(buffer)
            .Select(point => new ArrowCell(point, buffer.Get(point.X, point.Y).Glyph))
            .Where(t => t.Glyph is '▶' or '▷' or '◀' or '◁' or '▼' or '▽' or '▲' or '△');

    private static IReadOnlyList<string> RouteProjection(GraphRenderResult result)
        => result.Routes
            .Select(t => $"{t.Span.Edge}|{string.Join(",", t.Points)}")
            .ToArray();

    private static RenderedCell Describe(CanvasCell cell)
        => new(cell.Glyph, cell.Style, string.Join(",", cell.Owners.Select(t => ((GraphEdge)t).ToString())));

    private static GraphLayoutResult Frame(
        IReadOnlyList<GraphLayoutNode> nodes,
        IReadOnlyList<GraphLayoutEdgeSpan> spans,
        int crossings = 0,
        int reversedCount = 0)
        => new(nodes, spans, crossings, reversedCount);

    private static GraphLayoutNode Node(string id, int x, int y, int width, int height, int layer, int order)
        => new(id, x, y, width, height, layer, order);

    private static GraphLayoutEdgeSpan Span(
        string from,
        string to,
        int fromLayer,
        int toLayer,
        int fromOrder,
        int toOrder,
        GraphLayoutPoint fromPosition,
        GraphLayoutPoint toPosition,
        bool reversed = false,
        GraphEdgeKind kind = GraphEdgeKind.Blocks)
        => Span(Edge(from, to, kind), fromLayer, toLayer, fromOrder, toOrder, fromPosition, toPosition, reversed);

    private static GraphLayoutEdgeSpan Span(
        GraphEdge edge,
        int fromLayer,
        int toLayer,
        int fromOrder,
        int toOrder,
        GraphLayoutPoint fromPosition,
        GraphLayoutPoint toPosition,
        bool reversed = false)
        => new(edge, fromLayer, toLayer, fromOrder, toOrder, fromPosition, toPosition, reversed);

    private static GraphEdge Edge(string from, string to, GraphEdgeKind kind = GraphEdgeKind.Blocks)
        => new(from, to, kind);

    private sealed record RenderedCell(char Glyph, Style Style, string Owners);

    private sealed record ArrowCell(GraphLayoutPoint Point, char Glyph);
}
