using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a plan for fields associated with a delivery group set.
/// </summary>
public sealed class IncrementalPlan : IOperationPlan
{
    private readonly ExecutionNode?[] _nodesById;

    /// <summary>
    /// Initializes a new instance of <see cref="IncrementalPlan"/>.
    /// </summary>
    /// <param name="operation">
    /// The GraphQL operation associated with this incremental plan.
    /// </param>
    /// <param name="rootNodes">
    /// The root execution nodes that serve as entry points for this
    /// incremental plan.
    /// </param>
    /// <param name="allNodes">
    /// All execution nodes belonging to this incremental plan.
    /// </param>
    /// <param name="deliveryGroups">
    /// The delivery group set associated with this incremental plan, sorted
    /// ascending by <see cref="DeliveryGroup.Id"/>.
    /// </param>
    /// <param name="requirements">
    /// The requirements that this plan resolves from its enclosing plan scope
    /// before it executes.
    /// </param>
    public IncrementalPlan(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<OperationRequirement> requirements)
    {
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        DeliveryGroups = deliveryGroups;
        Requirements = requirements.IsDefault ? [] : requirements;
        _nodesById = CreateNodeLookup(allNodes);
    }

    /// <summary>
    /// Gets the unique identifier for this incremental plan.
    /// </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the GraphQL operation associated with this incremental plan.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for this
    /// incremental plan.
    /// </summary>
    public ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes belonging to this incremental plan.
    /// </summary>
    public ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets the delivery group set associated with this incremental plan,
    /// sorted ascending by <see cref="DeliveryGroup.Id"/>.
    /// </summary>
    public ImmutableArray<DeliveryGroup> DeliveryGroups { get; }

    /// <summary>
    /// Gets the requirements that this plan resolves from its enclosing plan
    /// scope before it executes. Each requirement maps a variable used by this
    /// plan to a selection in the enclosing scope's result.
    /// </summary>
    public ImmutableArray<OperationRequirement> Requirements { get; }

    /// <summary>
    /// Gets the <see cref="ExecutionNode.Id"/> of the node that produces the
    /// result object where this incremental plan is anchored. The identifier is
    /// scoped to the root <see cref="OperationPlan"/> for top-level plans, or
    /// to the enclosing <see cref="IncrementalPlan"/> for nested plans.
    /// </summary>
    public int ParentNodeId { get; internal set; }

    /// <summary>
    /// Gets the highest plan node identifier that can be resolved by this plan.
    /// </summary>
    public int MaxNodeId => _nodesById.Length > 0 ? _nodesById.Length - 1 : 0;

    /// <summary>
    /// Gets the child incremental plans for this plan. Incremental plans do not
    /// contain child plan objects; the root <see cref="OperationPlan"/> exposes
    /// the flat collection, and deferred fragment nesting is represented by
    /// <see cref="DeliveryGroup.Parent"/>.
    /// </summary>
    public ImmutableArray<IncrementalPlan> IncrementalPlans => [];

    /// <summary>
    /// Retrieves the execution node associated with a plan node identifier.
    /// </summary>
    /// <param name="id">
    /// The identifier of an execution node, or of an operation definition inside
    /// a batch.
    /// </param>
    /// <returns>The execution node associated with the specified identifier.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no node with the specified ID exists.</exception>
    public ExecutionNode GetNodeById(int id)
    {
        if ((uint)id < (uint)_nodesById.Length
            && _nodesById[id] is { } node)
        {
            return node;
        }

        throw ThrowHelper.NodeNotFound(id);
    }

    /// <summary>
    /// Returns the <see cref="ExecutionNode"/> responsible for executing the
    /// given plan node. If the plan node is already an execution node it is
    /// returned directly; if it is an operation definition inside a batch, the
    /// containing batch node is returned.
    /// </summary>
    public ExecutionNode GetExecutionNode(IOperationPlanNode planNode)
    {
        if (planNode is ExecutionNode executionNode)
        {
            return executionNode;
        }

        if ((uint)planNode.Id < (uint)_nodesById.Length
            && _nodesById[planNode.Id] is { } node)
        {
            return node;
        }

        throw ThrowHelper.NodeNotFound(planNode.Id);
    }

    private static ExecutionNode?[] CreateNodeLookup(ImmutableArray<ExecutionNode> allNodes)
    {
        if (allNodes.IsDefaultOrEmpty)
        {
            return [];
        }

        var maxId = 0;

        foreach (var node in allNodes)
        {
            maxId = Math.Max(maxId, node.Id);

            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var op in batchNode.Operations)
                {
                    maxId = Math.Max(maxId, op.Id);
                }
            }
        }

        var nodesById = new ExecutionNode?[maxId + 1];

        foreach (var node in allNodes)
        {
            nodesById[node.Id] = node;

            // Map each operation definition ID to the containing batch node,
            // so GetNodeById can resolve definition IDs to execution nodes.
            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var op in batchNode.Operations)
                {
                    nodesById[op.Id] = batchNode;
                }
            }
        }

        return nodesById;
    }
}
