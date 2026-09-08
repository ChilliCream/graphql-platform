namespace HotChocolate.CostAnalysis;

/// <summary>
/// The extracted condition tree of one selection-set boundary: a
/// hash-consed arena of nodes, one per distinct cumulative condition
/// reachable in that boundary.
/// </summary>
internal sealed class ConditionTree(int rootNodeId, IReadOnlyList<ConditionTreeNode> nodes)
{
    /// <summary>
    /// Gets the id of the boundary's root node.
    /// </summary>
    public int RootNodeId { get; } = rootNodeId;

    /// <summary>
    /// Gets every node in this tree, indexed by its id.
    /// </summary>
    public IReadOnlyList<ConditionTreeNode> Nodes { get; } = nodes;

    /// <summary>
    /// Gets the boundary's root node.
    /// </summary>
    public ConditionTreeNode Root => Nodes[RootNodeId];
}
