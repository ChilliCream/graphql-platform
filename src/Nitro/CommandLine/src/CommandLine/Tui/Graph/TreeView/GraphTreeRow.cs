using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;

/// <summary>
/// A visible row in the graph hierarchy tree, including its connector and
/// dependency summary information.
/// </summary>
internal sealed record GraphTreeRow
{
    /// <summary>
    /// The task represented by this row, or <see langword="null"/> for the
    /// virtual root.
    /// </summary>
    public required GraphNode? Task { get; init; }

    /// <summary>
    /// The existing dependency-tree row used to render this row's connector.
    /// </summary>
    public required TreeNodeRow Connector { get; init; }

    /// <summary>
    /// Whether the represented task has hierarchy children.
    /// </summary>
    public bool HasChildren { get; init; }

    /// <summary>
    /// Whether the represented task's hierarchy children are visible.
    /// </summary>
    public bool IsExpanded { get; init; }

    /// <summary>
    /// The count of matching descendant tasks hidden by a collapsed epic.
    /// </summary>
    public int ContainedMatchCount { get; init; }

    /// <summary>
    /// The number of nonterminal tasks in the reduced graph that block this task.
    /// </summary>
    public int BlockedByCount { get; init; }

    /// <summary>
    /// The number of nonterminal tasks in the reduced graph that this task blocks.
    /// </summary>
    public int BlocksCount { get; init; }

    /// <summary>
    /// The count of selected-task relationships hidden by a collapsed epic.
    /// </summary>
    public int ContainedRelationshipCount { get; init; }

    /// <summary>
    /// Whether this row is the current task selection.
    /// </summary>
    public bool IsSelected { get; init; }

    /// <summary>
    /// Whether this row has a blocking edge to or from the selection.
    /// </summary>
    public bool IsRelatedToSelection { get; init; }

    /// <summary>
    /// The task id for this row, or <see langword="null"/> for the root.
    /// </summary>
    public string? TaskId => Task?.Id;

    /// <summary>
    /// Whether this row represents the virtual root.
    /// </summary>
    public bool IsRoot => Task is null;
}
