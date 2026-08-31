using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// Controls which graph edges are drawn and how their strokes are styled.
/// </summary>
internal sealed class GraphEdgeRenderOptions
{
    public bool IncludeParentChild { get; init; }

    public Style BlocksStyle { get; init; } = ThemeTokens.GetStyle("board.column.border");

    public Style ParentChildStyle { get; init; } = ThemeTokens.GetStyle("detail.section.border");

    public Func<GraphEdge, Style?>? StyleOverride { get; init; }
}

/// <summary>
/// One routed span and the cells occupied by its stroke.
/// </summary>
internal sealed record GraphEdgeRoute(GraphLayoutEdgeSpan Span, IReadOnlyList<GraphLayoutPoint> Points);

/// <summary>
/// The routed graph canvas and its per-span geometry.
/// </summary>
internal sealed record GraphRenderResult(CellBuffer Buffer, IReadOnlyList<GraphEdgeRoute> Routes, GraphLayoutResult Layout)
{
    public CanvasViewport Viewport => new(0, 0, Buffer.Width, Buffer.Height);
}

/// <summary>
/// Routes layered graph spans through the whitespace between adjacent layers.
/// </summary>
internal sealed class GraphEdgeRouter
{
    public GraphRenderResult Route(GraphLayoutResult layout, GraphEdgeRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        options ??= new GraphEdgeRenderOptions();

        var nodesByPosition = layout.Nodes.ToDictionary(t => new GraphLayoutPoint(t.X, t.Y));
        var spans = layout.EdgeSpans
            .Where(t => t.Edge.Kind == GraphEdgeKind.Blocks || options.IncludeParentChild)
            .OrderBy(t => t.Edge.FromId, StringComparer.Ordinal)
            .ThenBy(t => t.Edge.ToId, StringComparer.Ordinal)
            .ThenBy(t => t.Edge.Kind)
            .ThenBy(t => t.FromLayer)
            .ThenBy(t => t.ToLayer)
            .ThenBy(t => t.FromPosition.Y)
            .ToArray();
        var geometries = new List<(GraphLayoutEdgeSpan Span, List<GraphLayoutPoint> Points, Style Style)>();
        var maxX = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.X + t.Width);
        var maxY = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.Y + t.Height);

        foreach (var group in spans.GroupBy(t => (t.FromLayer, t.ToLayer)))
        {
            var ordered = group
                .OrderBy(t => t.FromOrder)
                .ThenBy(t => t.ToOrder)
                .ThenBy(t => t.Edge.FromId, StringComparer.Ordinal)
                .ThenBy(t => t.Edge.ToId, StringComparer.Ordinal)
                .ThenBy(t => t.Edge.Kind)
                .ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var span = ordered[index];
                var start = SourcePort(span.FromPosition, nodesByPosition);
                var end = TargetPort(span.ToPosition, nodesByPosition);
                var channel = SelectChannel(start.X, end.X, index, ordered.Length);
                var points = Route(start, end, channel);
                var style = ResolveStyle(span, options);
                geometries.Add((span, points, style));
                maxX = Math.Max(maxX, points.Max(t => t.X) + 1);
                maxY = Math.Max(maxY, points.Max(t => t.Y) + 1);
            }
        }

        var buffer = new CellBuffer(maxX, maxY);
        var routes = new List<GraphEdgeRoute>(geometries.Count);
        foreach (var geometry in geometries)
        {
            DrawStroke(buffer, geometry.Span, geometry.Points, geometry.Style);
            routes.Add(new GraphEdgeRoute(geometry.Span, geometry.Points));
        }

        foreach (var geometry in geometries)
        {
            DrawArrow(buffer, geometry.Span, geometry.Points, geometry.Style);
        }

        return new GraphRenderResult(buffer, routes, layout);
    }

    private static GraphLayoutPoint SourcePort(
        GraphLayoutPoint position,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition)
        => nodesByPosition.TryGetValue(position, out var node)
            ? new GraphLayoutPoint(node.X + node.Width, node.Y + ((node.Height - 1) / 2))
            : position;

    private static GraphLayoutPoint TargetPort(
        GraphLayoutPoint position,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition)
        => nodesByPosition.TryGetValue(position, out var node)
            ? new GraphLayoutPoint(node.X - 1, node.Y + ((node.Height - 1) / 2))
            : position;

    private static int SelectChannel(int startX, int endX, int index, int count)
    {
        var low = Math.Min(startX, endX);
        var high = Math.Max(startX, endX);
        if (low == high)
        {
            return low;
        }

        var width = high - low + 1;
        return low + ((index * 2 + (count % 2)) % width);
    }

    private static List<GraphLayoutPoint> Route(GraphLayoutPoint start, GraphLayoutPoint end, int channel)
    {
        var points = new List<GraphLayoutPoint>();
        AddHorizontal(points, start.X, channel, start.Y);
        AddVertical(points, channel, start.Y, end.Y);
        AddHorizontal(points, channel, end.X, end.Y);
        return points;
    }

    private static void AddHorizontal(List<GraphLayoutPoint> points, int fromX, int toX, int y)
    {
        var step = fromX <= toX ? 1 : -1;
        for (var x = fromX; ; x += step)
        {
            Add(points, new GraphLayoutPoint(x, y));
            if (x == toX)
            {
                return;
            }
        }
    }

    private static void AddVertical(List<GraphLayoutPoint> points, int x, int fromY, int toY)
    {
        var step = fromY <= toY ? 1 : -1;
        for (var y = fromY; ; y += step)
        {
            Add(points, new GraphLayoutPoint(x, y));
            if (y == toY)
            {
                return;
            }
        }
    }

    private static void Add(List<GraphLayoutPoint> points, GraphLayoutPoint point)
    {
        if (points.Count == 0 || points[^1] != point)
        {
            points.Add(point);
        }
    }

    private static Style ResolveStyle(GraphLayoutEdgeSpan span, GraphEdgeRenderOptions options)
    {
        var style = options.StyleOverride?.Invoke(span.Edge)
            ?? (span.Edge.Kind == GraphEdgeKind.Blocks ? options.BlocksStyle : options.ParentChildStyle);
        return span.IsReversed
            ? new Style(style.Foreground, style.Background, style.Decoration | Decoration.Dim)
            : style;
    }

    private static void DrawStroke(
        CellBuffer buffer,
        GraphLayoutEdgeSpan span,
        IReadOnlyList<GraphLayoutPoint> points,
        Style style)
    {
        for (var index = 0; index < points.Count; index++)
        {
            var directions = CanvasDirections.None;
            if (index > 0)
            {
                directions |= Direction(points[index], points[index - 1]);
            }

            if (index < points.Count - 1)
            {
                directions |= Direction(points[index], points[index + 1]);
            }

            buffer.Connect(points[index].X, points[index].Y, directions, style, span.Edge, span.IsReversed);
        }
    }

    private static void DrawArrow(
        CellBuffer buffer,
        GraphLayoutEdgeSpan span,
        IReadOnlyList<GraphLayoutPoint> points,
        Style style)
    {
        var arrow = span.Edge.Kind == GraphEdgeKind.Blocks
            ? (span.IsReversed ? '◀' : '▶')
            : (span.IsReversed ? '◁' : '▷');
        var arrowPoint = span.IsReversed ? points[0] : points[^1];
        buffer.Set(arrowPoint.X, arrowPoint.Y, arrow, style);
    }

    private static CanvasDirections Direction(GraphLayoutPoint from, GraphLayoutPoint to)
    {
        if (to.X > from.X)
        {
            return CanvasDirections.Right;
        }

        if (to.X < from.X)
        {
            return CanvasDirections.Left;
        }

        if (to.Y > from.Y)
        {
            return CanvasDirections.Down;
        }

        return CanvasDirections.Up;
    }
}
