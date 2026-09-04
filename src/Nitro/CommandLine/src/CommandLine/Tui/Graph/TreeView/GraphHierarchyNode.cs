namespace ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;

/// <summary>
/// A node in the graph tree hierarchy. The root has no task and contains all
/// visible top-level nodes.
/// </summary>
internal sealed record GraphHierarchyNode(GraphNode? Task, IReadOnlyList<GraphHierarchyNode> Children)
{
    /// <summary>
    /// Whether this is the hierarchy's virtual root node.
    /// </summary>
    public bool IsRoot => Task is null;
}
