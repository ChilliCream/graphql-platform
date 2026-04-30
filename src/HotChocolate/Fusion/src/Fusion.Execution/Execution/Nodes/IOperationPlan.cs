using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents the common contract for operation plans.
/// </summary>
public interface IOperationPlan
{
    /// <summary>
    /// Gets the unique identifier for this plan.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the GraphQL operation associated with this plan.
    /// </summary>
    Operation Operation { get; }

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for this plan.
    /// </summary>
    ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes belonging to this plan, including both root and
    /// nested nodes.
    /// </summary>
    ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in
    /// ascending <see cref="DeliveryGroup.Id"/> order. Empty if the plan has no
    /// <c>@defer</c> directives in scope.
    /// </summary>
    ImmutableArray<DeliveryGroup> DeliveryGroups { get; }

    /// <summary>
    /// Gets the incremental plans associated with this plan.
    /// </summary>
    ImmutableArray<IncrementalPlan> IncrementalPlans { get; }

    /// <summary>
    /// Gets the highest plan node identifier that can be resolved by this plan.
    /// </summary>
    int MaxNodeId { get; }

    /// <summary>
    /// Retrieves the execution node associated with a plan node identifier.
    /// </summary>
    /// <param name="id">
    /// The identifier of an execution node, or of an operation definition inside
    /// a batch.
    /// </param>
    /// <returns>The execution node associated with the specified identifier.</returns>
    ExecutionNode GetNodeById(int id);

    /// <summary>
    /// Returns the <see cref="ExecutionNode"/> responsible for executing the
    /// given plan node. If the plan node is already an execution node it is
    /// returned directly; if it is a child operation (such as an operation
    /// definition inside a batch) the containing execution node is returned.
    /// </summary>
    /// <param name="planNode">The plan node to resolve.</param>
    /// <returns>The execution node that owns the given plan node.</returns>
    ExecutionNode GetExecutionNode(IOperationPlanNode planNode);
}
