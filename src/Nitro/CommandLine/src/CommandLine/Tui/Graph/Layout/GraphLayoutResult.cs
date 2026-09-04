namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// The complete result of one full graph layout frame.
/// </summary>
internal sealed record GraphLayoutResult(
    IReadOnlyList<GraphLayoutNode> Nodes,
    IReadOnlyList<GraphLayoutEdgeSpan> EdgeSpans,
    int CrossingCount,
    int ReversedEdgeCount)
{
    public GraphLayoutNode? FindNode(string id)
        => Nodes.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal));
}
