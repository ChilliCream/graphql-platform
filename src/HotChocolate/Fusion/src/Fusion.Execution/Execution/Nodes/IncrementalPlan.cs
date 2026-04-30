using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a plan for executing the fields that belong to a specific
/// <c>DeliveryGroupSet</c>. One <see cref="IncrementalPlan"/> is emitted per
/// unique non-empty active delivery group set in the operation. The set can
/// contain multiple <see cref="DeliveryGroup"/> instances, and the plan's data
/// is delivered to every group in <see cref="DeliveryGroups"/> when the
/// incremental plan completes.
/// </summary>
public sealed class IncrementalPlan : IOperationPlan
{
    private readonly ExecutionNode?[] _nodesById;

    /// <summary>
    /// Initializes a new instance of <see cref="IncrementalPlan"/>.
    /// </summary>
    /// <param name="operation">
    /// The compiled operation for this incremental plan's result mapping.
    /// </param>
    /// <param name="rootNodes">
    /// The root execution nodes that serve as entry points for this
    /// incremental plan.
    /// </param>
    /// <param name="allNodes">
    /// All execution nodes belonging to this incremental plan.
    /// </param>
    /// <param name="deliveryGroups">
    /// The <see cref="DeliveryGroup"/> set that keys this incremental plan,
    /// sorted ascending by <see cref="DeliveryGroup.Id"/>. One or more delivery
    /// groups can share this plan and receive its data on the wire when it
    /// completes.
    /// </param>
    /// <param name="requirements">
    /// The plan-scope requirements that must be supplied from the parent plan
    /// before this incremental plan can execute. Each requirement maps a
    /// variable name to a selection in the parent plan's result tree.
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
    /// Gets the unique identifier for this incremental plan. Assigned by the
    /// planner relative to the parent plan's id.
    /// </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the compiled operation for this incremental plan.
    /// This is a standalone operation compiled from the rewritten AST for this
    /// incremental plan, used for result mapping during execution.
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
    /// Gets the <see cref="DeliveryGroup"/> set that keys this incremental plan,
    /// sorted ascending by <see cref="DeliveryGroup.Id"/>. When this incremental
    /// plan completes, every <see cref="DeliveryGroup"/> in this set receives
    /// the incremental plan's data as an incremental payload on the wire. One
    /// plan can deliver to multiple delivery groups.
    /// </summary>
    public ImmutableArray<DeliveryGroup> DeliveryGroups { get; }

    /// <summary>
    /// Gets the plan-scope requirements that the parent plan must supply
    /// before this incremental plan can execute. Each requirement wires a
    /// variable used inside this incremental plan to a selection in the parent
    /// plan's result tree.
    /// </summary>
    public ImmutableArray<OperationRequirement> Requirements { get; }

    /// <summary>
    /// Gets the <see cref="ExecutionNode.Id"/> in the owning plan (the main plan
    /// for top-level incremental plans, the parent incremental plan for nested
    /// delivery groups) whose fetch resolves the selection set where this
    /// incremental plan was anchored.
    /// Always populated for a sealed plan; query plan visualizers can use this
    /// to attach the incremental plan to the node that produces its enclosing
    /// data.
    /// Set during plan construction.
    /// </summary>
    public int ParentNodeId { get; internal set; }

    /// <summary>
    /// Gets the maximum <see cref="ExecutionNode.Id"/> across all nodes in this
    /// incremental plan. Used by the executor's context pooling to size
    /// per-invocation node arrays without reallocation.
    /// </summary>
    public int MaxNodeId => _nodesById.Length > 0 ? _nodesById.Length - 1 : 0;

    /// <summary>
    /// Gets the child incremental execution plans for this plan. Incremental
    /// plans do not nest at the plan level; nesting is modeled via
    /// <see cref="DeliveryGroup.Parent"/> on delivery groups. This property is
    /// always empty on an incremental plan and exists to satisfy the
    /// <see cref="IOperationPlan"/> contract.
    /// </summary>
    public ImmutableArray<IncrementalPlan> IncrementalPlans => [];

    /// <summary>
    /// Retrieves an execution node by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the execution node is unique within this
    /// incremental plan.
    /// </param>
    /// <returns>The execution node with the specified identifier.</returns>
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
    /// Returns the <see cref="ExecutionNode"/> that is responsible for executing
    /// the given plan node.
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
