using System.Collections.Immutable;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a group of execution nodes that correspond to a single <c>@defer</c> fragment.
/// The gateway executes these nodes after the initial (non-deferred) result has been sent
/// and streams the result back as an incremental payload.
/// </summary>
public sealed class DeferredExecutionGroup
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeferredExecutionGroup"/>.
    /// </summary>
    /// <param name="deferId">
    /// A unique identifier for this deferred payload, used in <c>pending</c> and <c>completed</c> entries.
    /// </param>
    /// <param name="label">
    /// The optional label from <c>@defer(label: "...")</c>.
    /// </param>
    /// <param name="path">
    /// The path in the result tree where deferred data will be inserted.
    /// </param>
    /// <param name="ifVariable">
    /// The variable name from <c>@defer(if: $var)</c>, or <c>null</c> if unconditional.
    /// </param>
    /// <param name="parent">
    /// The parent deferred group for nested <c>@defer</c>, or <c>null</c> for top-level.
    /// </param>
    /// <param name="operation">
    /// The compiled operation for this deferred group's result mapping.
    /// </param>
    /// <param name="rootNodes">
    /// The root execution nodes that serve as entry points for this deferred group.
    /// </param>
    /// <param name="allNodes">
    /// All execution nodes belonging to this deferred group.
    /// </param>
    /// <param name="siblingOverlapByResponseName">
    /// Optional map of response names that this group selects which are also
    /// selected by at least one sibling defer, to the declaration-order list of
    /// competing <see cref="DeferId"/>s (this group's own id included). <c>null</c>
    /// when there is no sibling overlap (the fast path).
    /// </param>
    public DeferredExecutionGroup(
        int deferId,
        string? label,
        SelectionPath path,
        string? ifVariable,
        DeferredExecutionGroup? parent,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableDictionary<string, ImmutableArray<int>>? siblingOverlapByResponseName = null)
    {
        DeferId = deferId;
        Label = label;
        Path = path;
        IfVariable = ifVariable;
        Parent = parent;
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        SiblingOverlapByResponseName = siblingOverlapByResponseName;
    }

    /// <summary>
    /// Gets the unique identifier for this deferred payload.
    /// This ID is used in <c>pending</c>, <c>incremental</c>, and <c>completed</c> response entries.
    /// </summary>
    public int DeferId { get; }

    /// <summary>
    /// Gets the optional label from <c>@defer(label: "...")</c>.
    /// </summary>
    public string? Label { get; }

    /// <summary>
    /// Gets the path in the result tree where deferred data will be inserted.
    /// </summary>
    public SelectionPath Path { get; }

    /// <summary>
    /// Gets the variable name from <c>@defer(if: $var)</c>,
    /// or <c>null</c> if this defer is unconditional.
    /// </summary>
    public string? IfVariable { get; }

    /// <summary>
    /// Gets the parent deferred group for nested <c>@defer</c>,
    /// or <c>null</c> for top-level deferred groups.
    /// </summary>
    public DeferredExecutionGroup? Parent { get; }

    /// <summary>
    /// Gets the <see cref="ExecutionNode.Id"/> in the owning plan (the main plan
    /// for top-level defers, the parent defer group's plan for nested defers)
    /// whose fetch resolves the selection set where this defer was anchored.
    /// Always populated for a sealed plan; query plan visualizers can use this
    /// to attach the deferred group to the node that produces its enclosing
    /// data. Set during plan construction.
    /// </summary>
    public int ParentNodeId { get; internal set; }

    /// <summary>
    /// Gets the compiled operation for this deferred group.
    /// This is a standalone operation compiled from the deferred fragment's AST,
    /// used for result mapping during execution.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for this deferred group.
    /// </summary>
    public ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes belonging to this deferred group.
    /// </summary>
    public ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets the sibling-defer overlap map for this group. The key is the response
    /// name of a field that this group selects; the value is the list of
    /// <see cref="DeferId"/>s (including this group's own) of sibling defers that
    /// select the same response name, in declaration order (ascending DeferId).
    /// At runtime the "earliest completing" sibling wins the field and others
    /// must drop it from their incremental payload. <c>null</c> when there is
    /// no overlap with any sibling (the fast path).
    /// </summary>
    public ImmutableDictionary<string, ImmutableArray<int>>? SiblingOverlapByResponseName { get; }
}
