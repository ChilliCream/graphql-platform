using System.Text;

namespace ChilliCream.Nitro.CommandLine.Tui.Tree;

/// <summary>
/// One flattened row of a dependency tree: the task it names, its depth from
/// the tree's root, and enough sibling-position state to draw its
/// box-drawing connector without consulting any other row.
/// </summary>
internal sealed class TreeNodeRow
{
    /// <summary>
    /// The id of the task this row names.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// The row's distance from the tree's root. The root itself is depth 0.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Whether this row is the last of its siblings under its parent.
    /// Meaningless, and always <see langword="true"/>, for the root.
    /// </summary>
    public bool IsLastChild { get; init; } = true;

    /// <summary>
    /// For each ancestor strictly between the root and this row's parent,
    /// whether that ancestor was itself the last child of its own parent.
    /// Empty for the root and for its direct children.
    /// </summary>
    public IReadOnlyList<bool> AncestorIsLastChild { get; init; } = [];

    /// <summary>
    /// Whether this row is a repeat occurrence of a task already reached
    /// earlier, higher up, in the same traversal. A cycle row is rendered
    /// but never expanded further.
    /// </summary>
    public bool IsCycle { get; init; }

    /// <summary>
    /// Builds the plain-text box-drawing connector this row's badge should
    /// be prefixed with: empty for the root, otherwise a run of vertical-bar
    /// or blank segments tracking which ancestors still have siblings below
    /// them, followed by this row's own branch glyph.
    /// </summary>
    public string BuildConnector()
    {
        if (Depth == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(AncestorIsLastChild.Count * 3 + 3);

        foreach (var ancestorIsLast in AncestorIsLastChild)
        {
            builder.Append(ancestorIsLast ? "   " : "│  ");
        }

        builder.Append(IsLastChild ? "└─ " : "├─ ");
        return builder.ToString();
    }
}
