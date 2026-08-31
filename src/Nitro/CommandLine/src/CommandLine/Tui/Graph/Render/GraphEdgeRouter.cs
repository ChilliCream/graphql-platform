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
    public int RenderedEdgeCount { get; init; }

    public CanvasViewport Viewport => new(0, 0, Buffer.Width, Buffer.Height);
}

/// <summary>
/// Routes layered graph spans through the whitespace between adjacent layers.
/// </summary>
internal sealed class GraphEdgeRouter
{
    public GraphRenderResult Route(GraphLayoutResult layout, GraphEdgeRenderOptions? options = null)
    {
        options ??= new GraphEdgeRenderOptions();

        var nodesByPosition = layout.Nodes.ToDictionary(t => new GraphLayoutPoint(t.X, t.Y));
        var nodesByLayer = layout.Nodes
            .GroupBy(t => t.Layer)
            .ToDictionary(t => t.Key, t => (IReadOnlyList<GraphLayoutNode>)t.ToArray());
        var semanticEdges = layout.EdgeSpans
            .Where(t => t.Edge.Kind == GraphEdgeKind.Blocks || options.IncludeParentChild)
            .GroupBy(t => t.Edge)
            .OrderBy(t => t.Key.FromId, StringComparer.Ordinal)
            .ThenBy(t => t.Key.ToId, StringComparer.Ordinal)
            .ThenBy(t => t.Key.Kind)
            .Select((group, ordinal) => new SemanticEdge(
                group.Key,
                ordinal,
                options.StyleOverride?.Invoke(group.Key),
                group.OrderBy(t => t.FromLayer)
                    .ThenBy(t => t.ToLayer)
                    .ThenBy(t => t.FromOrder)
                    .ThenBy(t => t.ToOrder)
                    .ThenBy(t => t.FromPosition.Y)
                    .ThenBy(t => t.ToPosition.Y)
                    .ToArray()))
            .ToArray();
        var spans = semanticEdges.SelectMany(t => t.Spans.Select(span => new OrderedSpan(span, t.Ordinal))).ToArray();
        var geometryBySpan = new Dictionary<GraphLayoutEdgeSpan, RoutedSpan>();
        var maxX = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.X + t.Width);
        var maxY = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.Y + t.Height);

        foreach (var group in spans.GroupBy(t => (t.Span.FromLayer, t.Span.ToLayer)))
        {
            var ordered = group
                .OrderBy(t => t.Span.FromOrder)
                .ThenBy(t => t.Span.ToOrder)
                .ThenBy(t => t.Ordinal)
                .ThenBy(t => t.Span.FromPosition.Y)
                .ToArray();
            nodesByLayer.TryGetValue(group.Key.FromLayer, out var sourceNodes);
            nodesByLayer.TryGetValue(group.Key.ToLayer, out var targetNodes);
            sourceNodes ??= [];
            targetNodes ??= [];

            for (var index = 0; index < ordered.Length; index++)
            {
                var orderedSpan = ordered[index];
                var span = orderedSpan.Span;
                var start = SourcePort(span.FromPosition, nodesByPosition);
                var end = TargetPort(span.ToPosition, nodesByPosition);
                var points = FindRoute(start, end, index, sourceNodes, targetNodes, layout.Nodes);
                var semanticEdge = semanticEdges[orderedSpan.Ordinal];
                var contribution = ResolveContribution(
                    span,
                    orderedSpan.Ordinal,
                    semanticEdge.OverrideStyle,
                    options);
                geometryBySpan.Add(span, new RoutedSpan(span, points, contribution));
                maxX = Math.Max(maxX, points.Max(t => t.X) + 1);
                maxY = Math.Max(maxY, points.Max(t => t.Y) + 1);
            }
        }

        var buffer = new CellBuffer(maxX, maxY);
        var routes = new List<GraphEdgeRoute>(spans.Length);
        foreach (var semanticEdge in semanticEdges)
        {
            foreach (var span in semanticEdge.Spans)
            {
                var geometry = geometryBySpan[span];
                DrawStroke(buffer, geometry);
                routes.Add(new GraphEdgeRoute(span, geometry.Points));
            }
        }

        foreach (var semanticEdge in semanticEdges)
        {
            DrawArrow(buffer, semanticEdge, geometryBySpan, nodesByPosition);
        }

        return new GraphRenderResult(buffer, routes, layout) { RenderedEdgeCount = semanticEdges.Length };
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

    private static List<GraphLayoutPoint> FindRoute(
        GraphLayoutPoint start,
        GraphLayoutPoint end,
        int index,
        IReadOnlyList<GraphLayoutNode> sourceNodes,
        IReadOnlyList<GraphLayoutNode> targetNodes,
        IReadOnlyList<GraphLayoutNode> allNodes)
    {
        var sourceBound = sourceNodes.Count == 0
            ? Math.Min(start.X, end.X)
            : sourceNodes.Max(t => t.X + t.Width);
        var targetBound = targetNodes.Count == 0
            ? Math.Max(start.X, end.X)
            : targetNodes.Min(t => t.X);
        var low = Math.Min(sourceBound, targetBound);
        var high = Math.Max(sourceBound, targetBound) - 1;
        if (high < low)
        {
            low = Math.Min(start.X, end.X);
            high = Math.Max(start.X, end.X);
        }

        var candidateCount = high - low + 1;
        for (var offset = 0; offset < candidateCount; offset++)
        {
            var channel = low + ((index + offset) % candidateCount);
            var points = Route(start, end, channel);
            if (IsClear(points, allNodes))
            {
                return points;
            }
        }

        return FindGridRoute(start, end, allNodes);
    }

    private static List<GraphLayoutPoint> FindGridRoute(
        GraphLayoutPoint start,
        GraphLayoutPoint end,
        IReadOnlyList<GraphLayoutNode> nodes)
    {
        var minX = Math.Max(0, Math.Min(start.X, end.X) - 1);
        var maxX = Math.Max(Math.Max(start.X, end.X), nodes.Count == 0 ? 0 : nodes.Max(t => t.X + t.Width)) + 1;
        var minY = Math.Max(0, Math.Min(start.Y, end.Y) - 1);
        var maxY = Math.Max(Math.Max(start.Y, end.Y), nodes.Count == 0 ? 0 : nodes.Max(t => t.Y + t.Height)) + 1;
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var previous = new int[width * height];
        Array.Fill(previous, -2);
        var queue = new Queue<int>();
        var startIndex = ToIndex(start.X, start.Y, minX, minY, width);
        var endIndex = ToIndex(end.X, end.Y, minX, minY, width);
        previous[startIndex] = -1;
        queue.Enqueue(startIndex);

        ReadOnlySpan<(int X, int Y)> steps = [(1, 0), (0, 1), (-1, 0), (0, -1)];
        while (queue.Count > 0 && previous[endIndex] == -2)
        {
            var current = queue.Dequeue();
            var x = (current % width) + minX;
            var y = (current / width) + minY;
            foreach (var step in steps)
            {
                var nextX = x + step.X;
                var nextY = y + step.Y;
                if (nextX < minX || nextX > maxX || nextY < minY || nextY > maxY || Contains(nodes, nextX, nextY))
                {
                    continue;
                }

                var next = ToIndex(nextX, nextY, minX, minY, width);
                if (previous[next] != -2)
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        if (previous[endIndex] == -2)
        {
            return Route(start, end, start.X);
        }

        var points = new List<GraphLayoutPoint>();
        for (var current = endIndex; current >= 0; current = previous[current])
        {
            points.Add(new GraphLayoutPoint((current % width) + minX, (current / width) + minY));
        }

        points.Reverse();
        return points;
    }

    private static int ToIndex(int x, int y, int minX, int minY, int width) => ((y - minY) * width) + x - minX;

    private static bool IsClear(IEnumerable<GraphLayoutPoint> points, IReadOnlyList<GraphLayoutNode> nodes)
        => points.All(point => !Contains(nodes, point.X, point.Y));

    private static bool Contains(IReadOnlyList<GraphLayoutNode> nodes, int x, int y)
    {
        foreach (var node in nodes)
        {
            if (x >= node.X && x < node.X + node.Width && y >= node.Y && y < node.Y + node.Height)
            {
                return true;
            }
        }

        return false;
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

    private static EdgeContribution ResolveContribution(
        GraphLayoutEdgeSpan span,
        int ordinal,
        Style? overrideStyle,
        GraphEdgeRenderOptions options)
    {
        var style = overrideStyle
            ?? (span.Edge.Kind == GraphEdgeKind.Blocks ? options.BlocksStyle : options.ParentChildStyle);
        if (span.IsReversed)
        {
            style = new Style(style.Foreground, style.Background, style.Decoration | Decoration.Dim);
        }

        return new EdgeContribution(
            style,
            span.IsReversed,
            overrideStyle is not null ? 3 : span.Edge.Kind == GraphEdgeKind.ParentChild ? 2 : 1,
            ordinal);
    }

    private static void DrawStroke(CellBuffer buffer, RoutedSpan geometry)
    {
        for (var index = 0; index < geometry.Points.Count; index++)
        {
            var directions = CanvasDirections.None;
            if (index > 0)
            {
                directions |= Direction(geometry.Points[index], geometry.Points[index - 1]);
            }

            if (index < geometry.Points.Count - 1)
            {
                directions |= Direction(geometry.Points[index], geometry.Points[index + 1]);
            }

            buffer.Connect(
                geometry.Points[index].X,
                geometry.Points[index].Y,
                directions,
                geometry.Contribution.Style,
                geometry.Span.Edge,
                geometry.Contribution.Dashed,
                geometry.Contribution.Rank,
                geometry.Contribution.Ordinal);
        }
    }

    private static void DrawArrow(
        CellBuffer buffer,
        SemanticEdge semanticEdge,
        IReadOnlyDictionary<GraphLayoutEdgeSpan, RoutedSpan> geometryBySpan,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition)
    {
        var reversed = semanticEdge.Spans[0].IsReversed;
        var span = reversed
            ? semanticEdge.Spans.FirstOrDefault(t => IsNodeAt(t.FromPosition, semanticEdge.Edge.ToId, nodesByPosition))
            : semanticEdge.Spans.LastOrDefault(t => IsNodeAt(t.ToPosition, semanticEdge.Edge.ToId, nodesByPosition));
        span ??= reversed ? semanticEdge.Spans[0] : semanticEdge.Spans[^1];
        var geometry = geometryBySpan[span];
        var point = reversed ? geometry.Points[0] : geometry.Points[^1];
        var arrow = semanticEdge.Edge.Kind == GraphEdgeKind.Blocks
            ? (reversed ? '◀' : '▶')
            : (reversed ? '◁' : '▷');
        buffer.SetArrow(
            point.X,
            point.Y,
            arrow,
            geometry.Contribution.Style,
            semanticEdge.Edge,
            geometry.Contribution.Dashed,
            geometry.Contribution.Rank,
            geometry.Contribution.Ordinal);
    }

    private static bool IsNodeAt(
        GraphLayoutPoint position,
        string id,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition)
        => nodesByPosition.TryGetValue(position, out var node) && string.Equals(node.Id, id, StringComparison.Ordinal);

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

    private sealed record SemanticEdge(
        GraphEdge Edge,
        int Ordinal,
        Style? OverrideStyle,
        IReadOnlyList<GraphLayoutEdgeSpan> Spans);

    private readonly record struct OrderedSpan(GraphLayoutEdgeSpan Span, int Ordinal);

    private readonly record struct EdgeContribution(Style Style, bool Dashed, int Rank, int Ordinal);

    private sealed record RoutedSpan(
        GraphLayoutEdgeSpan Span,
        List<GraphLayoutPoint> Points,
        EdgeContribution Contribution);
}
