namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Assigns each acyclic graph vertex to a left-to-right layer.
/// </summary>
internal interface IGraphLayeringStrategy
{
    IReadOnlyDictionary<string, int> AssignLayers(
        IReadOnlyList<string> nodeIds,
        IReadOnlyList<LayoutArc> arcs,
        GraphLayoutResult? previousLayout);
}
