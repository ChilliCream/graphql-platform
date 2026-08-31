namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// The deterministic graph model consumed by the graph projections and
/// layout pipeline.
/// </summary>
internal sealed record GraphModel(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges,
    bool IsReduced = false,
    int HiddenNodeCount = 0);
