namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents the query plan node kinds.
/// </summary>
internal enum QueryPlanNodeKind
{
    /// <summary>
    /// The <see cref="Parallel"/> node executes its child nodes in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// The <see cref="Sequence"/> node executes its child nodes sequentially.
    /// </summary>
    Sequence,

    /// <summary>
    /// The resolver node is responsible for fetching data from a subgraph.
    /// </summary>
    Resolve,

    /// <summary>
    /// The resolver node is responsible for batch fetching data from a subgraph.
    /// </summary>
    ResolveByKeyBatch,

    /// <summary>
    /// The resolver node is responsible for fetching nodes from subgraphs.
    /// </summary>
    ResolveNode,

    /// <summary>
    /// A subscribe represents a subscription operation that is executed on a subgraph.
    /// </summary>
    Subscribe,

    /// <summary>
    /// The introspection node is responsible for fetching the schema from a subgraph.
    /// </summary>
    Introspect,

    /// <summary>
    /// The <see cref="Compose"/> node composes the results of multiple selection sets.
    /// </summary>
    Compose,

    /// <summary>
    /// The <see cref="If"/> node executes its child nodes based on a condition.
    /// </summary>
    If
}
