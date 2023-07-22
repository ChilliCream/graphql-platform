namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The sequence node is responsible for executing
/// it's child nodes in a sequence one after the other.
/// </summary>
internal sealed class Sequence : QueryPlanNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="Sequence"/>.
    /// </summary>
    /// <param name="id">
    /// The unique id of this node.
    /// <remarks>Unique withing its query plan.</remarks>
    /// </param>
    public Sequence(int id) : base(id) { }

    /// <summary>
    /// Gets the kind of this node.
    /// </summary>
    public override QueryPlanNodeKind Kind => QueryPlanNodeKind.Sequence;
}
