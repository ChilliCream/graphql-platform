namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// One adjacent-layer segment of a graph edge after layered layout.
/// </summary>
internal sealed record GraphLayoutEdgeSpan(
    GraphEdge Edge,
    int FromLayer,
    int ToLayer,
    int FromOrder,
    int ToOrder,
    GraphLayoutPoint FromPosition,
    GraphLayoutPoint ToPosition,
    bool IsReversed);
