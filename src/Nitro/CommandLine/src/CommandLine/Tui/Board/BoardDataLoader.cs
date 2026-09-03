using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Evaluates a board column against the task store: translates its
/// declarative filter into a <see cref="TaskFilter"/> query, applies the
/// computed ready/blocked/deferred semantics the filter cannot express
/// alone, then sorts and caps the result. Issues no SQL of its own.
/// </summary>
internal sealed class BoardDataLoader(ITaskStore store, TimeProvider timeProvider)
{
    /// <summary>
    /// Stands in for a blocked map for the Deferred filter below, which never
    /// consults it: <see cref="TaskBoardStatus.Resolve"/> resolves deferred
    /// ahead of blocked, so its result for "is this task deferred" never
    /// depends on the blocked set. Avoids a redundant
    /// <see cref="ITaskStore.ComputeBlockedAsync"/> round trip.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> s_noBlockers =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Loads the tasks currently matching one column.
    /// </summary>
    public async Task<IReadOnlyList<TaskItem>> LoadColumnAsync(
        ColumnDefinition column,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var isUnassigned = IsUnassigned(column.Assignee);

        var filter = new TaskFilter
        {
            Statuses = column.Statuses,
            IncludeAll = column.ComputedFilter == ColumnComputedFilter.Blocked,
            Assignee = isUnassigned ? null : column.Assignee,
            Unassigned = isUnassigned,
            Labels = column.Labels,
            ExcludeBlocked = column.ComputedFilter == ColumnComputedFilter.Ready,
            DeferredVisibleAt = column.ComputedFilter == ColumnComputedFilter.Ready ? now : null
        };

        var tasks = (IEnumerable<TaskItem>)await store.QueryTasksAsync(filter, cancellationToken);

        if (column.ComputedFilter == ColumnComputedFilter.Blocked)
        {
            var blocked = await store.ComputeBlockedAsync(cancellationToken);
            tasks = tasks.Where(t => TaskBoardStatus.Resolve(t, blocked, now) == TaskStates.Blocked);
        }
        else if (column.ComputedFilter == ColumnComputedFilter.Deferred)
        {
            tasks = tasks.Where(t => TaskBoardStatus.Resolve(t, s_noBlockers, now) == TaskStates.Deferred);
        }

        if (column.Types is { Length: > 0 } types)
        {
            tasks = tasks.Where(t => types.Contains(t.Type));
        }

        if (column.Priorities is { Length: > 0 } priorities)
        {
            tasks = tasks.Where(t => priorities.Contains(t.Priority));
        }

        var sorted = Sort(tasks, column.Sort);

        return column.Limit is { } limit ? sorted.Take(limit).ToList() : sorted.ToList();
    }

    private static bool IsUnassigned(string? assignee)
        => string.Equals(assignee, "unassigned", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<TaskItem> Sort(IEnumerable<TaskItem> tasks, BoardColumnSort sort)
        => sort switch
        {
            BoardColumnSort.RecentFirst => tasks
                .OrderByDescending(t => t.ClosedAt ?? t.UpdatedAt)
                .ThenBy(t => t.Id, StringComparer.Ordinal),
            _ => tasks
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ThenBy(t => t.Id, StringComparer.Ordinal)
        };
}
