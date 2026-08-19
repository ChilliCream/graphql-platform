using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// A computed membership test a column applies on top of its declarative
/// filter, for semantics that <see cref="TaskFilter"/> cannot express alone.
/// </summary>
internal enum ColumnComputedFilter
{
    /// <summary>
    /// No computed test; the declarative filter alone decides membership.
    /// </summary>
    None,

    /// <summary>
    /// Open, unblocked, and not deferred into the future.
    /// </summary>
    Ready,

    /// <summary>
    /// Not closed or tombstoned, and blocked by an unresolved dependency.
    /// </summary>
    Blocked,

    /// <summary>
    /// Deferred: either its status is deferred, or its defer date is in the
    /// future.
    /// </summary>
    Deferred
}

/// <summary>
/// How a column orders the tasks it loads.
/// </summary>
internal enum BoardColumnSort
{
    /// <summary>
    /// Priority ascending, then created date ascending, then id ascending,
    /// matching the list task command's default order.
    /// </summary>
    Default,

    /// <summary>
    /// Closed date descending (falling back to updated date), then id
    /// ascending.
    /// </summary>
    RecentFirst
}

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
    /// Resolves this column's border and header style: its own accent token,
    /// brightened when <paramref name="focused"/> is true, or the default
    /// neutral border tokens when no accent is assigned.
    /// </summary>
    public Style ResolveBorderStyle(bool focused)
    {
        var token = BorderToken is { } accent
            ? focused ? $"{accent}.focused" : accent
            : focused ? "board.column.border.focused" : "board.column.border";

        return ThemeTokens.GetStyle(token);
    }
}
