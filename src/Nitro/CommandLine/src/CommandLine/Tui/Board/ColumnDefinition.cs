using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// A named board column: a declarative task filter plus how the matching
/// tasks are sorted and how many are kept.
/// </summary>
internal sealed record ColumnDefinition
{
    /// <summary>
    /// The column's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Statuses a task must have one of. Null means every non-terminal
    /// status.
    /// </summary>
    public string[]? Statuses { get; init; }

    /// <summary>
    /// Task types a task must have one of. Null means every type.
    /// </summary>
    public string[]? Types { get; init; }

    /// <summary>
    /// Priorities a task must have one of. Null means every priority.
    /// </summary>
    public int[]? Priorities { get; init; }

    /// <summary>
    /// An assignee a task must have, or "unassigned" to match tasks with no
    /// assignee. Null means every assignee.
    /// </summary>
    public string? Assignee { get; init; }

    /// <summary>
    /// Labels a task must all carry. Null means no label requirement.
    /// </summary>
    public string[]? Labels { get; init; }

    /// <summary>
    /// The computed membership test applied on top of the fields above.
    /// </summary>
    public ColumnComputedFilter ComputedFilter { get; init; } = ColumnComputedFilter.None;

    /// <summary>
    /// The order matching tasks are sorted in.
    /// </summary>
    public BoardColumnSort Sort { get; init; } = BoardColumnSort.Default;

    /// <summary>
    /// The maximum number of tasks kept after sorting. Null means unlimited.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The theme token for this column's border and header accent color, for
    /// example <c>board.column.status.blocked</c>. Null falls back to the
    /// board's default neutral border tokens.
    /// </summary>
    public string? BorderToken { get; init; }

    /// <summary>
    /// Resolves this column's border and header style: its own accent
    /// token's focused or unfocused variant, or the default neutral border
    /// tokens when no accent is assigned. Focus is shown by the pane's box
    /// weight and header boldness (see <see cref="Widgets.PaneBorders"/>),
    /// not by this style, which stays the same accent color either way
    /// except where a status token's own focused variant differs.
    /// </summary>
    public Style ResolveBorderStyle(bool focused)
    {
        var token = BorderToken is { } accent
            ? focused ? $"{accent}.focused" : accent
            : focused ? "board.column.border.focused" : "board.column.border";

        return ThemeTokens.GetStyle(token);
    }
}
