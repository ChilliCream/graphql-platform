namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// An edge in the direction used by the layout pipeline.
/// </summary>
internal sealed class LayoutArc
{
    public LayoutArc(GraphEdge edge, string fromId, string toId, bool isReversed)
    {
        Edge = edge;
        FromId = fromId;
        ToId = toId;
        IsReversed = isReversed;
    }

    public GraphEdge Edge { get; }

    public string FromId { get; set; }

    public string ToId { get; set; }

    public bool IsReversed { get; set; }
}
