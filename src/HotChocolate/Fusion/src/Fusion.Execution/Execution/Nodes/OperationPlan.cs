using System.Collections.Immutable;
using System.Security.Cryptography;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a GraphQL operation execution plan in Hot Chocolate Fusion, containing
/// the structured nodes and metadata required for distributed query execution.
/// </summary>
public sealed record OperationPlan
{
    private static readonly JsonOperationPlanFormatter s_formatter = new();
    private readonly ExecutionNode?[] _nodesById = [];

    private OperationPlan(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        int searchSpace,
        int expandedNodes)
    {
        Id = id;
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        SearchSpace = searchSpace;
        ExpandedNodes = expandedNodes;
        _nodesById = CreateNodeLookup(allNodes);
        MaxNodeId = _nodesById.Length > 0 ? _nodesById.Length - 1 : 0;
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
    /// Gets the maximum node identifier across all nodes in this plan.
    /// </summary>
    public int MaxNodeId { get; }

    /// <summary>
    /// Retrieves an execution node by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the execution node is unique within this plan.</param>
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
    /// <para>
    /// Returns the <see cref="ExecutionNode"/> that is responsible for executing
    /// the given plan node.
    /// </para>
    /// <para>
    /// If the plan node is already an execution node it is returned directly.
    /// If it is a child operation plan node (such as an <see cref="OperationDefinition"/>
    /// inside a batch) this method returns the parent execution node that is
    /// responsible for its execution.
    /// </para>
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
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node arrays have negative length.</exception>
    public static OperationPlan Create(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(rootNodes.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(allNodes.Length, 0);

        return new OperationPlan(id, operation, rootNodes, allNodes, searchSpace, expandedNodes);
    }

    /// <summary>
    /// Creates a new operation plan with an auto-generated identifier based on the operation's content.
    /// The identifier is generated by computing a SHA256 hash of the serialized operation plan.
    /// </summary>
    /// <param name="operation">The GraphQL operation.</param>
    /// <param name="rootNodes">The root execution nodes.</param>
    /// <param name="allNodes">All execution nodes in the plan.</param>
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance with a content-based identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node arrays have negative length.</exception>
    public static OperationPlan Create(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
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

        return new OperationPlan(id, operation, rootNodes, allNodes, searchSpace, expandedNodes);
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
