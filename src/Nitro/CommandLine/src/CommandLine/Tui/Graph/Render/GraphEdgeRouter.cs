using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

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
            .ThenBy(t => t.Key.IsReversed)
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
        var arrows = new Dictionary<GraphEdge, ArrowGeometry>();
        var acceptedRouteCells = new HashSet<GraphLayoutPoint>();
        var reservedArrowCells = new HashSet<GraphLayoutPoint>();
        var maxX = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.X + t.Width);
        var maxY = layout.Nodes.Count == 0 ? 0 : layout.Nodes.Max(t => t.Y + t.Height);

        foreach (var semanticEdge in semanticEdges)
        {
            var pending = new List<RoutedSpan>(semanticEdge.Spans.Count);
            GraphLayoutPoint? pendingArrowPoint = null;
            RoutePort? arrowPort = null;
            EdgeContribution? arrowContribution = null;
            var routed = true;
            for (var index = 0; index < semanticEdge.Spans.Count; index++)
            {
                var span = semanticEdge.Spans[index];
                nodesByLayer.TryGetValue(span.FromLayer, out var sourceNodes);
                nodesByLayer.TryGetValue(span.ToLayer, out var targetNodes);
                sourceNodes ??= [];
                targetNodes ??= [];
                var isFromTarget = IsNodeAt(span.FromPosition, semanticEdge.Edge.ToId, nodesByPosition);
                var isToTarget = IsNodeAt(span.ToPosition, semanticEdge.Edge.ToId, nodesByPosition);

                if (!TryFindRoute(
                    span,
                    index,
                    nodesByPosition,
                    sourceNodes,
                    targetNodes,
                    layout.Nodes,
                    acceptedRouteCells,
                    reservedArrowCells,
                    pendingArrowPoint,
                    isFromTarget,
                    isToTarget,
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

                if (TryGetArrowPort(span, semanticEdge.Edge.ToId, route, nodesByPosition, out var selectedArrowPort))
                {
                    if (arrowPort is not null
                        || selectedArrowPort.Direction == CanvasDirections.None
                        || acceptedRouteCells.Contains(selectedArrowPort.Point)
                        || IsReserved(selectedArrowPort.Point, reservedArrowCells, pendingArrowPoint))
                    {
                        routed = false;
                        break;
                    }

                    arrowPort = selectedArrowPort;
                    arrowContribution = contribution;
                    pendingArrowPoint = selectedArrowPort.Point;
                }
            }

            if (!routed || arrowPort is null || arrowContribution is null)
            {
                continue;
            }

            foreach (var geometry in pending)
            {
                geometryBySpan.Add(geometry.Span, geometry);
                maxX = Math.Max(maxX, geometry.Points.Max(t => t.X) + 1);
                maxY = Math.Max(maxY, geometry.Points.Max(t => t.Y) + 1);
                foreach (var point in geometry.Points)
                {
                    acceptedRouteCells.Add(point);
                }
            }

            arrows.Add(semanticEdge.Edge, new ArrowGeometry(arrowPort.Value, arrowContribution.Value));
            reservedArrowCells.Add(arrowPort.Value.Point);
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
            if (arrows.TryGetValue(semanticEdge.Edge, out var arrow))
            {
                DrawArrow(buffer, semanticEdge, arrow);
            }
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
        HashSet<GraphLayoutPoint> acceptedRouteCells,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        bool isFromTarget,
        bool isToTarget,
        out RouteGeometry route)
    {
        var starts = GetPorts(span.FromPosition, span.ToPosition, nodesByPosition, allNodes);
        var ends = GetPorts(span.ToPosition, span.FromPosition, nodesByPosition, allNodes);
        foreach (var start in starts)
        {
            foreach (var end in ends)
            {
                if ((isFromTarget && (acceptedRouteCells.Contains(start.Point)
                        || IsReserved(start.Point, reservedArrowCells, pendingArrowPoint)))
                    || (isToTarget && (acceptedRouteCells.Contains(end.Point)
                        || IsReserved(end.Point, reservedArrowCells, pendingArrowPoint))))
                {
                    continue;
                }

                if (TryFindRoute(
                    start,
                    end,
                    index,
                    sourceNodes,
                    targetNodes,
                    allNodes,
                    reservedArrowCells,
                    pendingArrowPoint,
                    out var points))
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

    private static bool TryGetAnchor(
        RoutePort port,
        IReadOnlyList<GraphLayoutNode> nodes,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        out GraphLayoutPoint anchor)
    {
        anchor = port.Direction switch
        {
            CanvasDirections.Right => new GraphLayoutPoint(port.Point.X - 1, port.Point.Y),
            CanvasDirections.Left => new GraphLayoutPoint(port.Point.X + 1, port.Point.Y),
            CanvasDirections.Down => new GraphLayoutPoint(port.Point.X, port.Point.Y - 1),
            CanvasDirections.Up => new GraphLayoutPoint(port.Point.X, port.Point.Y + 1),
            _ => port.Point
        };

        return anchor.X >= 0
            && anchor.Y >= 0
            && !Contains(nodes, anchor.X, anchor.Y)
            && !IsReserved(anchor, reservedArrowCells, pendingArrowPoint);
    }

    private static List<GraphLayoutPoint> ComposeRoute(
        GraphLayoutPoint startPort,
        GraphLayoutPoint startAnchor,
        IReadOnlyList<GraphLayoutPoint> interior,
        GraphLayoutPoint endAnchor,
        GraphLayoutPoint endPort)
    {
        var route = new List<GraphLayoutPoint>(interior.Count + 4);
        Add(route, startPort);
        Add(route, startAnchor);
        foreach (var point in interior)
        {
            Add(route, point);
        }

        Add(route, endAnchor);
        Add(route, endPort);
        return route;
    }

    private static bool TryFindRoute(
        RoutePort start,
        RoutePort end,
        int index,
        IReadOnlyList<GraphLayoutNode> sourceNodes,
        IReadOnlyList<GraphLayoutNode> targetNodes,
        IReadOnlyList<GraphLayoutNode> allNodes,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        out List<GraphLayoutPoint> route)
    {
        if (IsReserved(start.Point, reservedArrowCells, pendingArrowPoint)
            || IsReserved(end.Point, reservedArrowCells, pendingArrowPoint)
            || !TryGetAnchor(start, allNodes, reservedArrowCells, pendingArrowPoint, out var startAnchor)
            || !TryGetAnchor(end, allNodes, reservedArrowCells, pendingArrowPoint, out var endAnchor)
            || startAnchor == end.Point
            || endAnchor == start.Point)
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
            var points = Route(startAnchor, endAnchor, channel);
            if (IsClearRoute(points, allNodes, reservedArrowCells, pendingArrowPoint, start, end, startAnchor, endAnchor))
            {
                route = ComposeRoute(start.Point, startAnchor, points, endAnchor, end.Point);
                return true;
            }
        }

        if (TryFindGridRoute(
                startAnchor,
                endAnchor,
                allNodes,
                reservedArrowCells,
                pendingArrowPoint,
                start,
                end,
                out var gridRoute)
            && IsClearRoute(gridRoute, allNodes, reservedArrowCells, pendingArrowPoint, start, end, startAnchor, endAnchor))
        {
            route = ComposeRoute(start.Point, startAnchor, gridRoute, endAnchor, end.Point);
            return true;
        }

        route = [];
        return false;
    }

    private static bool TryFindGridRoute(
        GraphLayoutPoint start,
        GraphLayoutPoint end,
        IReadOnlyList<GraphLayoutNode> nodes,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        RoutePort startPort,
        RoutePort endPort,
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
                var nextPoint = new GraphLayoutPoint(nextX, nextY);
                if (nextX < 0
                    || nextX > maxX
                    || nextY < 0
                    || nextY > maxY
                    || Contains(nodes, nextX, nextY)
                    || (nextPoint != end && IsBlocked(nextPoint, reservedArrowCells, pendingArrowPoint, startPort, endPort)))
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
        return IsClear(route, nodes, reservedArrowCells, pendingArrowPoint);
    }

    private static int ToIndex(int x, int y, int width) => (y * width) + x;

    private static bool IsClear(
        IEnumerable<GraphLayoutPoint> points,
        IReadOnlyList<GraphLayoutNode> nodes,
        HashSet<GraphLayoutPoint>? reservedArrowCells = null,
        GraphLayoutPoint? pendingArrowPoint = null)
        => points.All(point => !Contains(nodes, point.X, point.Y)
            && (reservedArrowCells is null || !IsReserved(point, reservedArrowCells, pendingArrowPoint)));

    private static bool IsClearRoute(
        IEnumerable<GraphLayoutPoint> points,
        IReadOnlyList<GraphLayoutNode> nodes,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        RoutePort start,
        RoutePort end,
        GraphLayoutPoint startAnchor,
        GraphLayoutPoint endAnchor)
        => points.All(point => !Contains(nodes, point.X, point.Y)
            && !IsReserved(point, reservedArrowCells, pendingArrowPoint)
            && (point == startAnchor
                || point == endAnchor
                || !IsBlocked(point, reservedArrowCells, pendingArrowPoint, start, end)));

    private static bool IsBlocked(
        GraphLayoutPoint point,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint,
        RoutePort start,
        RoutePort end)
        => IsReserved(point, reservedArrowCells, pendingArrowPoint)
            || (start.Direction != CanvasDirections.None && point == start.Point)
            || (end.Direction != CanvasDirections.None && point == end.Point);

    private static bool IsReserved(
        GraphLayoutPoint point,
        HashSet<GraphLayoutPoint> reservedArrowCells,
        GraphLayoutPoint? pendingArrowPoint)
        => reservedArrowCells.Contains(point)
            || (pendingArrowPoint is { } pending && pending == point);

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
            style = GraphEdgeStyles.Dim(style);
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
        ArrowGeometry arrow)
    {
        buffer.SetArrow(
            arrow.Port.Point.X,
            arrow.Port.Point.Y,
            ArrowFor(arrow.Port.Direction, semanticEdge.Edge.Kind),
            arrow.Contribution.Style,
            semanticEdge.Edge,
            arrow.Contribution.Dashed,
            arrow.Contribution.Rank,
            arrow.Contribution.Ordinal);
    }

    private static bool TryGetArrowPort(
        GraphLayoutEdgeSpan span,
        string targetId,
        RouteGeometry route,
        IReadOnlyDictionary<GraphLayoutPoint, GraphLayoutNode> nodesByPosition,
        out RoutePort port)
    {
        if (IsNodeAt(span.FromPosition, targetId, nodesByPosition))
        {
            port = route.Start;
            return true;
        }

        if (IsNodeAt(span.ToPosition, targetId, nodesByPosition))
        {
            port = route.End;
            return true;
        }

        port = default;
        return false;
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

    private readonly record struct ArrowGeometry(RoutePort Port, EdgeContribution Contribution);

    private sealed record RoutedSpan(
        GraphLayoutEdgeSpan Span,
        List<GraphLayoutPoint> Points,
        EdgeContribution Contribution,
        RoutePort Start,
        RoutePort End);
}
