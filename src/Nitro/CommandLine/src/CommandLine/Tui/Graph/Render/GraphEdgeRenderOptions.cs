namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// Controls which graph edges are drawn and how their strokes are styled.
/// </summary>
internal sealed class GraphEdgeRenderOptions
{
    public bool IncludeParentChild { get; init; }

    public Style BlocksStyle { get; init; } = GraphEdgeStyles.Line;

    public Style ParentChildStyle { get; init; } = GraphEdgeStyles.Line;

    public Func<GraphEdge, Style?>? StyleOverride { get; init; }
}
