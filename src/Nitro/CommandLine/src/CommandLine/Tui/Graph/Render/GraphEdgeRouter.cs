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
                group.Distinct()
                    .OrderBy(t => t.FromLayer)
                    .ThenBy(t => t.ToLayer)
                    .ThenBy(t => t.FromOrder)
                    .ThenBy(t => t.ToOrder)
                    .ThenBy(t => t.FromPosition.X)
                    .ThenBy(t => t.FromPosition.Y)
                    .ThenBy(t => t.ToPosition.X)
                    .ThenBy(t => t.ToPosition.Y)
                    .ThenBy(t => t.IsReversed)
                    .ToArray()))
            .ToArray();
        var spanCount = semanticEdges.Sum(t => t.Spans.Count);
        var geometryBySpan = new Dictionary<GraphLayoutEdgeSpan, RoutedSpan>();
        var maxX = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.X + t.Width);
        var maxY = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.Y + t.Height);

        foreach (var semanticEdge in semanticEdges)
        {
            var pending = new List<RoutedSpan>(semanticEdge.Spans.Count);
            var routed = true;
            for (var index = 0; index < semanticEdge.Spans.Count; index++)
            {
                var span = semanticEdge.Spans[index];
                nodesByLayer.TryGetValue(span.FromLayer, out var sourceNodes);
                nodesByLayer.TryGetValue(span.ToLayer, out var targetNodes);
                sourceNodes ??= [];
                targetNodes ??= [];

                if (!TryFindRoute(
                    span,
                    index,
                    nodesByPosition,
                    sourceNodes,
                    targetNodes,
                    layout.Nodes,
                    out var route))
                {
                    routed = false;
                    break;
                }

                var contribution = ResolveContribution(
                    span,
                    semanticEdge.Ordinal,
                    semanticEdge.OverrideStyle,
                    options);
                pending.Add(new RoutedSpan(span, route.Points, contribution, route.Start, route.End));
            }

            if (!routed)
            {
                continue;
            }

            foreach (var geometry in pending)
            {
                geometryBySpan.Add(geometry.Span, geometry);
                maxX = Math.Max(maxX, geometry.Points.Max(t => t.X) + 1);
                maxY = Math.Max(maxY, geometry.Points.Max(t => t.Y) + 1);
            }
        }

        var buffer = new CellBuffer(maxX, maxY);
        var routes = new List<GraphEdgeRoute>(spanCount);
        foreach (var semanticEdge in semanticEdges)
        {
            if (semanticEdge.Spans.Any(t => !geometryBySpan.ContainsKey(t)))
            {
                continue;
            }

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

        return new GraphRenderResult(buffer, routes, layout)
        {
            RenderedEdgeCount = semanticEdges.Count(t => t.Spans.All(geometryBySpan.ContainsKey))
        };
    }

    private static bool TryFindRoute(
        GraphLayoutEdgeSpan span,
        int index,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition,
        IReadOnlyList<GraphLayoutNode> sourceNodes,
        IReadOnlyList<GraphLayoutNode> targetNodes,
        IReadOnlyList<GraphLayoutNode> allNodes,
        out RouteGeometry route)
    {
        var starts = GetPorts(span.FromPosition, span.ToPosition, nodesByPosition, allNodes);
        var ends = GetPorts(span.ToPosition, span.FromPosition, nodesByPosition, allNodes);
        foreach (var start in starts)
        {
            foreach (var end in ends)
            {
                if (TryFindRoute(start, end, index, sourceNodes, targetNodes, allNodes, out var points))
                {
                    route = new RouteGeometry(points, start, end);
                    return true;
                }
            }
        }

        route = default;
        return false;
    }

    private static IReadOnlyList<RoutePort> GetPorts(
        GraphLayoutPoint position,
        GraphLayoutPoint adjacentPosition,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition,
        IReadOnlyList<GraphLayoutNode> nodes)
    {
        if (!nodesByPosition.TryGetValue(position, out var node))
        {
            return IsClear([position], nodes) && position.X >= 0 && position.Y >= 0
                ? [new RoutePort(position, CanvasDirections.None)]
                : [];
        }

        var preferred = adjacentPosition.X >= node.X ? CanvasDirections.Right : CanvasDirections.Left;
        var sides = preferred == CanvasDirections.Right
            ? new[] { CanvasDirections.Right, CanvasDirections.Left, CanvasDirections.Down, CanvasDirections.Up }
            : new[] { CanvasDirections.Left, CanvasDirections.Right, CanvasDirections.Down, CanvasDirections.Up };
        var ports = new List<RoutePort>((node.Width + node.Height) * 2);
        foreach (var side in sides)
        {
            foreach (var port in GetSidePorts(node, side))
            {
                if (port.Point.X >= 0 && port.Point.Y >= 0 && !Contains(nodes, port.Point.X, port.Point.Y))
                {
                    ports.Add(port);
                }
            }
        }

        return ports;
    }

    private static IEnumerable<RoutePort> GetSidePorts(GraphLayoutNode node, CanvasDirections side)
    {
        if (side is CanvasDirections.Left or CanvasDirections.Right)
        {
            var x = side == CanvasDirections.Left ? node.X - 1 : node.X + node.Width;
            foreach (var portY in CenterOut(node.Y, node.Height))
            {
                yield return new RoutePort(
                    new GraphLayoutPoint(x, portY),
                    side == CanvasDirections.Left ? CanvasDirections.Right : CanvasDirections.Left);
            }

            yield break;
        }

        var y = side == CanvasDirections.Up ? node.Y - 1 : node.Y + node.Height;
        foreach (var x in CenterOut(node.X, node.Width))
        {
            yield return new RoutePort(
                new GraphLayoutPoint(x, y),
                side == CanvasDirections.Up ? CanvasDirections.Down : CanvasDirections.Up);
        }
    }

    private static IEnumerable<int> CenterOut(int origin, int length)
    {
        var center = origin + ((length - 1) / 2);
        yield return center;
        for (var offset = 1; offset < length; offset++)
        {
            var before = center - offset;
            if (before >= origin)
            {
                yield return before;
            }

            var after = center + offset;
            if (after < origin + length)
            {
                yield return after;
            }
        }
    }

    private static bool TryFindRoute(
        RoutePort start,
        RoutePort end,
        int index,
        IReadOnlyList<GraphLayoutNode> sourceNodes,
        IReadOnlyList<GraphLayoutNode> targetNodes,
        IReadOnlyList<GraphLayoutNode> allNodes,
        out List<GraphLayoutPoint> route)
    {
        if (Contains(allNodes, start.Point.X, start.Point.Y) || Contains(allNodes, end.Point.X, end.Point.Y))
        {
            route = [];
            return false;
        }

        var sourceBound = sourceNodes.Count == 0
            ? Math.Min(start.Point.X, end.Point.X)
            : sourceNodes.Max(t => t.X + t.Width);
        var targetBound = targetNodes.Count == 0
            ? Math.Max(start.Point.X, end.Point.X)
            : targetNodes.Min(t => t.X);
        var low = Math.Min(sourceBound, targetBound);
        var high = Math.Max(sourceBound, targetBound) - 1;
        if (high < low)
        {
            low = Math.Min(start.Point.X, end.Point.X);
            high = Math.Max(start.Point.X, end.Point.X);
        }

        var candidateCount = high - low + 1;
        for (var offset = 0; offset < candidateCount; offset++)
        {
            var channel = low + ((index + offset) % candidateCount);
            var points = Route(start.Point, end.Point, channel);
            if (IsClear(points, allNodes))
            {
                route = points;
                return true;
            }
        }

        return TryFindGridRoute(start.Point, end.Point, allNodes, out route);
    }

    private static bool TryFindGridRoute(
        GraphLayoutPoint start,
        GraphLayoutPoint end,
        IReadOnlyList<GraphLayoutNode> nodes,
        out List<GraphLayoutPoint> route)
    {
        var maxX = Math.Max(Math.Max(start.X, end.X), nodes.Count == 0 ? 0 : nodes.Max(t => t.X + t.Width)) + 1;
        var maxY = Math.Max(Math.Max(start.Y, end.Y), nodes.Count == 0 ? 0 : nodes.Max(t => t.Y + t.Height)) + 1;
        var width = maxX + 1;
        var height = maxY + 1;
        var previous = new int[width * height];
        Array.Fill(previous, -2);
        var queue = new Queue<int>();
        var startIndex = ToIndex(start.X, start.Y, width);
        var endIndex = ToIndex(end.X, end.Y, width);
        previous[startIndex] = -1;
        queue.Enqueue(startIndex);

        ReadOnlySpan<(int X, int Y)> steps = [(1, 0), (0, 1), (-1, 0), (0, -1)];
        while (queue.Count > 0 && previous[endIndex] == -2)
        {
            var current = queue.Dequeue();
            var x = current % width;
            var y = current / width;
            foreach (var step in steps)
            {
                var nextX = x + step.X;
                var nextY = y + step.Y;
                if (nextX < 0 || nextX > maxX || nextY < 0 || nextY > maxY || Contains(nodes, nextX, nextY))
                {
                    continue;
                }

                var next = ToIndex(nextX, nextY, width);
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
            route = [];
            return false;
        }

        route = [];
        for (var current = endIndex; current >= 0; current = previous[current])
        {
            route.Add(new GraphLayoutPoint(current % width, current / width));
        }

        route.Reverse();
        return IsClear(route, nodes);
    }

    private static int ToIndex(int x, int y, int width) => (y * width) + x;

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
        foreach (var span in semanticEdge.Spans)
        {
            if (!geometryBySpan.TryGetValue(span, out var geometry))
            {
                return;
            }

            RoutePort port;
            if (IsNodeAt(span.FromPosition, semanticEdge.Edge.ToId, nodesByPosition))
            {
                port = geometry.Start;
            }
            else if (IsNodeAt(span.ToPosition, semanticEdge.Edge.ToId, nodesByPosition))
            {
                port = geometry.End;
            }
            else
            {
                continue;
            }

            if (port.Direction == CanvasDirections.None)
            {
                return;
            }

            buffer.SetArrow(
                port.Point.X,
                port.Point.Y,
                ArrowFor(port.Direction, semanticEdge.Edge.Kind),
                geometry.Contribution.Style,
                semanticEdge.Edge,
                geometry.Contribution.Dashed,
                geometry.Contribution.Rank,
                geometry.Contribution.Ordinal);
            return;
        }
    }

    private static char ArrowFor(CanvasDirections direction, GraphEdgeKind kind)
    {
        var filled = kind == GraphEdgeKind.Blocks;
        return direction switch
        {
            CanvasDirections.Right => filled ? '▶' : '▷',
            CanvasDirections.Left => filled ? '◀' : '◁',
            CanvasDirections.Down => filled ? '▼' : '▽',
            CanvasDirections.Up => filled ? '▲' : '△',
            _ => ' '
        };
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

    private readonly record struct EdgeContribution(Style Style, bool Dashed, int Rank, int Ordinal);

    private readonly record struct RoutePort(GraphLayoutPoint Point, CanvasDirections Direction);

    private readonly record struct RouteGeometry(
        List<GraphLayoutPoint> Points,
        RoutePort Start,
        RoutePort End);

    private sealed record RoutedSpan(
        GraphLayoutEdgeSpan Span,
        List<GraphLayoutPoint> Points,
        EdgeContribution Contribution,
        RoutePort Start,
        RoutePort End);
}
