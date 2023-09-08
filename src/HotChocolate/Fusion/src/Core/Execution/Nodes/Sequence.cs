namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The sequence node is responsible for executing
/// it's child nodes in a sequence one after the other.
/// </summary>
/// <param name="id">
/// The unique id of this node.
/// <remarks>Unique withing its query plan.</remarks>
/// </param>
internal sealed class Sequence(int id) : QueryPlanNode(id)
{
    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Sequence;
}
