using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// A named board layout made of ordered columns.
/// </summary>
internal sealed record BoardView
{
    /// <summary>
    /// The maximum number of tasks kept in the closed column, most recently
    /// closed first.
    /// </summary>
    private const int ClosedColumnLimit = 50;

    /// <summary>
    /// The board's display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The board's columns, in display order.
    /// </summary>
    public required IReadOnlyList<ColumnDefinition> Columns { get; init; }

    /// <summary>
    /// The v1 built-in board view: Blocked, Deferred, Ready, In Progress, and
    /// Closed columns, matching the semantics of the blocked, ready, and list
    /// task commands.
    /// </summary>
    public static BoardView Default { get; } = new()
    {
        Name = "Board",
        Columns =
        [
            new ColumnDefinition
            {
                Name = "Blocked",
                ComputedFilter = ColumnComputedFilter.Blocked,
                BorderToken = "board.column.status.blocked"
            },
            new ColumnDefinition
            {
                Name = "Deferred",
                ComputedFilter = ColumnComputedFilter.Deferred,
                BorderToken = "board.column.status.deferred"
            },
            new ColumnDefinition
            {
                Name = "Ready",
                Statuses = [TaskStates.Open],
                ComputedFilter = ColumnComputedFilter.Ready,
                BorderToken = "board.column.status.ready"
            },
            new ColumnDefinition
            {
                Name = "In Progress",
                Statuses = [TaskStates.InProgress],
                BorderToken = "board.column.status.inprogress"
            },
            new ColumnDefinition
            {
                Name = "Closed",
                Statuses = [TaskStates.Closed],
                Sort = BoardColumnSort.RecentFirst,
                Limit = ClosedColumnLimit,
                BorderToken = "board.column.status.closed"
            }
        ]
    };
}
