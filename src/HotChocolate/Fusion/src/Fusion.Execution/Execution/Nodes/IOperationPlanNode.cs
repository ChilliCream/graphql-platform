namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a node in the <see cref="OperationPlan"/> dependency graph.
/// </summary>
public interface IOperationPlanNode
{
    /// <summary>
    /// Gets the unique identifier of this node within the operation plan.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the nodes that depend on this node. These nodes cannot
    /// start executing until this node has completed.
    /// </summary>
    ReadOnlySpan<IOperationPlanNode> Dependents { get; }

    /// <summary>
    /// Gets the nodes that this node requires to have completed
    /// before it can start executing. If any required dependency
    /// is skipped or fails, this node will be skipped as well.
    /// </summary>
    ReadOnlySpan<IOperationPlanNode> Dependencies { get; }

    /// <summary>
    /// Gets the nodes that this node optionally depends on.
    /// This node will still execute even if an optional dependency
    /// is skipped or fails.
    /// </summary>
    ReadOnlySpan<IOperationPlanNode> OptionalDependencies { get; }
}
