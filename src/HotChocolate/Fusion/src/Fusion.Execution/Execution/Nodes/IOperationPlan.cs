using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents the shared executor contract exposed by both root operation plans
/// and incremental execution subplans. Consumers in the executor pipeline (context
/// pooling, plan iteration, incremental delivery) depend only on this surface;
/// plan-specific metrics and subplan-specific state remain on the concrete types.
/// </summary>
public interface IOperationPlan
{
    /// <summary>
    /// Gets the unique identifier for this plan. The root plan's id is assigned by
    /// the planner (for example, <c>"main"</c> or a content-addressed hash), while
    /// a subplan's id is derived from its parent plan's id.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the GraphQL operation associated with this plan. For the root plan this
    /// is the incoming operation; for a subplan this is the compiled operation used
    /// for the subplan's result mapping.
    /// </summary>
    Operation Operation { get; }

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for this plan. On
    /// the root plan these are the entry nodes of the full operation; on a subplan
    /// these are the subplan's own entry nodes.
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
    /// Gets the incremental execution subplans emitted for this plan. Populated on the
    /// root plan, empty on a subplan (subplans do not nest at the plan level; nesting
    /// is modeled via <see cref="DeliveryGroup.Parent"/> on delivery groups).
    /// </summary>
    ImmutableArray<IncrementalPlan> IncrementalPlans { get; }

    /// <summary>
    /// Gets the maximum <see cref="ExecutionNode.Id"/> across all nodes in this
    /// plan. Used by the executor's context pooling to size per-invocation node
    /// arrays without reallocation.
    /// </summary>
    int MaxNodeId { get; }

    /// <summary>
    /// Retrieves an execution node by its unique identifier within this plan.
    /// </summary>
    /// <param name="id">The unique identifier of the execution node.</param>
    /// <returns>The execution node with the specified identifier.</returns>
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
