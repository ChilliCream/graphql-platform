namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// The relationship carried by a graph edge.
/// </summary>
internal enum GraphEdgeKind
{
    Blocks,
    ParentChild
}

/// <summary>
/// A directed relationship between graph nodes. <see cref="FromId"/> is the
/// prerequisite or parent, and <see cref="ToId"/> is the dependent or child.
/// </summary>
internal sealed record GraphEdge(
    string FromId,
    string ToId,
    GraphEdgeKind Kind,
    bool IsReversed = false);
