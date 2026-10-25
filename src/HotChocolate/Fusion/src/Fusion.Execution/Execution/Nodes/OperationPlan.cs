using System.Collections.Immutable;
using System.Security.Cryptography;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Policies;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a GraphQL operation execution plan in Hot Chocolate Fusion, containing
/// the structured nodes and metadata required for distributed query execution.
/// </summary>
public sealed partial record OperationPlan : IOperationPlan
{
    private static readonly JsonOperationPlanFormatter s_formatter = new();
    private readonly ExecutionNode?[] _nodesById = [];

    private OperationPlan(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        PolicyPlan? policies,
        int searchSpace,
        int expandedNodes)
    {
        Id = id;
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        SearchSpace = searchSpace;
        ExpandedNodes = expandedNodes;
        DeliveryGroups = deliveryGroups;
        IncrementalPlans = incrementalPlans;
        _deliveryPaths = CreateDeliveryPaths(incrementalPlans, deliveryGroups.Length);
        _nodesById = CreateNodeLookup(
            allNodes,
            out var usesDynamicSchemaNames,
            out var usesBatchNodes);
        MaxNodeId = _nodesById.Length > 0 ? _nodesById.Length - 1 : 0;
        UsesDynamicSchemaNames = usesDynamicSchemaNames;
        UsesBatchNodes = usesBatchNodes;
        Policies = policies;

        if (EntityReuseCandidateMap.Create(this) is { } entityReuseCandidateMap)
        {
            operation.Features.Set(entityReuseCandidateMap);
        }
    }

    /// <summary>
    /// Gets the unique identifier for this operation plan.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the GraphQL operation associated with this execution plan.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the variable definitions from the operation.
    /// </summary>
    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions
        => Operation.Definition.VariableDefinitions;

    /// <summary>
    /// Gets the name of the operation, or <c>null</c> if the operation is anonymous.
    /// </summary>
    public string? OperationName => Operation.Name;

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for query execution.
    /// </summary>
    public ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes in the plan, including both root and nested nodes.
    /// </summary>
    public ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets a number specifying how many possible plans were considered during planning.
    /// </summary>
    public int SearchSpace { get; }

    /// <summary>
    /// Gets the number of nodes expanded (dequeued) during the A* search.
    /// </summary>
    public int ExpandedNodes { get; }

    /// <summary>
    /// Gets every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in
    /// ascending <see cref="DeliveryGroup.Id"/> order. One element per <c>@defer</c>
    /// occurrence in the operation. Empty if the operation has no <c>@defer</c>
    /// directives.
    /// </summary>
    public ImmutableArray<DeliveryGroup> DeliveryGroups { get; }

    /// <summary>
    /// Gets the incremental plans for this plan. This is a flat collection;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// Empty if the operation has no <c>@defer</c> directives.
    /// </summary>
    public ImmutableArray<IncrementalPlan> IncrementalPlans { get; }

    /// <summary>
    /// Gets the highest plan node identifier that can be resolved by this plan.
    /// </summary>
    public int MaxNodeId { get; }

    internal bool UsesDynamicSchemaNames { get; }

    internal bool UsesBatchNodes { get; }

    /// <summary>
    /// Gets the policy requirements of the operation and its incremental plans,
    /// or <c>null</c> if nothing is protected.
    /// </summary>
    public PolicyPlan? Policies { get; }

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

    /// <summary>
    /// Creates a new operation plan with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the operation plan.</param>
    /// <param name="operation">The GraphQL operation.</param>
    /// <param name="rootNodes">The root execution nodes.</param>
    /// <param name="allNodes">All execution nodes in the plan.</param>
    /// <param name="deliveryGroups">
    /// Every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in ascending
    /// <see cref="DeliveryGroup.Id"/> order.
    /// </param>
    /// <param name="incrementalPlans">
    /// The incremental plans for <c>@defer</c> support. The collection is flat;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// </param>
    /// <param name="policies">
    /// The policy requirements of the operation and its incremental plans, or <c>null</c> if nothing is protected.
    /// </param>
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node collections are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a delivery path cannot be resolved.</exception>
    public static OperationPlan Create(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        PolicyPlan? policies,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(rootNodes.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(allNodes.Length, 0);

        return new OperationPlan(
            id, operation, rootNodes, allNodes, deliveryGroups, incrementalPlans, policies, searchSpace, expandedNodes);
    }

    /// <summary>
    /// Creates a new operation plan with an identifier derived from the plan content.
    /// </summary>
    /// <param name="operation">The GraphQL operation.</param>
    /// <param name="rootNodes">The root execution nodes.</param>
    /// <param name="allNodes">All execution nodes in the plan.</param>
    /// <param name="deliveryGroups">
    /// Every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in ascending
    /// <see cref="DeliveryGroup.Id"/> order.
    /// </param>
    /// <param name="incrementalPlans">
    /// The incremental plans for <c>@defer</c> support. The collection is flat;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// </param>
    /// <param name="policies">
    /// The policy requirements of the operation and its incremental plans, or <c>null</c> if nothing is protected.
    /// </param>
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance with a content-based identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node collections are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a delivery path cannot be resolved.</exception>
    public static OperationPlan Create(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        PolicyPlan? policies,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(rootNodes.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(allNodes.Length, 0);

        using var buffer = new PooledArrayWriter(initialBufferSize: 4096);
        s_formatter.Format(buffer, operation, allNodes);

        // Generate a unique identifier for the operation plan by hashing its serialized form.
        // The hash is appended to the same buffer to reuse the already-allocated memory.
        var hashDestination = buffer.GetSpan(32);

        SHA256.HashData(buffer.WrittenSpan, hashDestination);
        buffer.Advance(32);

#if NET9_0_OR_GREATER
        var id = Convert.ToHexStringLower(buffer.WrittenSpan[^32..]);
#else
        var id = Convert.ToHexString(buffer.WrittenSpan[^32..]).ToLowerInvariant();
#endif

        return new OperationPlan(
            id,
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            policies,
            searchSpace,
            expandedNodes);
    }

    private static ExecutionNode?[] CreateNodeLookup(
        ImmutableArray<ExecutionNode> allNodes,
        out bool usesDynamicSchemaNames,
        out bool usesBatchNodes)
    {
        usesDynamicSchemaNames = false;
        usesBatchNodes = false;

        if (allNodes.IsDefaultOrEmpty)
        {
            return [];
        }

        var maxId = 0;

        foreach (var node in allNodes)
        {
            maxId = Math.Max(maxId, node.Id);

            switch (node.Type)
            {
                case ExecutionNodeType.Node:
                    usesDynamicSchemaNames = true;
                    break;

                case ExecutionNodeType.OperationBatch:
                    usesBatchNodes = true;
                    break;
            }

            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var op in batchNode.Operations)
                {
                    maxId = Math.Max(maxId, op.Id);
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatchNode)
            {
                foreach (var op in apolloBatchNode.Operations)
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

            if (node is ApolloOperationBatchExecutionNode apolloBatchNode)
            {
                foreach (var op in apolloBatchNode.Operations)
                {
                    nodesById[op.Id] = apolloBatchNode;
                }
            }
        }

        return nodesById;
    }
}
