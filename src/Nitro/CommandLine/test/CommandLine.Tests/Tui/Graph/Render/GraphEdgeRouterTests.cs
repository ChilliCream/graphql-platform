using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
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
        Assert.Empty(intrusions);
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
            [Span("dependency", "dependent", 0, 1, 0, 0, new GraphLayoutPoint(0, 0), new GraphLayoutPoint(8, 2), reversed: true)]);

        // act
        var result = new GraphEdgeRouter().Route(layout);
        var route = Assert.Single(result.Routes);

        // assert
        Assert.Equal('◀', result.Buffer.Get(route.Points[0].X, route.Points[0].Y).Glyph);
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
            new GraphEdgeRenderOptions { StyleOverride = edge => edge == first ? new Style(Color.Red) : null });
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
}
