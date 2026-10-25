using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// The structured result of parsing one search-mode query line, produced by
/// <see cref="TaskQueryParser"/>. Maps onto the store's <see cref="TaskFilter"/>
/// through <see cref="ToFilter"/>, the single place that translates between
/// the two shapes.
/// </summary>
internal sealed record TaskQuery
{
    /// <summary>
    /// A query with every field at its default, equivalent to an empty
    /// input line.
    /// </summary>
    public static readonly TaskQuery Empty = new();

    /// <summary>
    /// The lookback window the <see cref="Stale"/> keyword applies, matching
    /// the task stale command's default threshold.
    /// </summary>
    public static readonly TimeSpan DefaultStaleWindow = TimeSpan.FromDays(30);

    /// <summary>
    /// Free text accumulated from bare words, joined with a single space in
    /// the order they appeared. Null when no bare words were given.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Statuses from repeated status: tokens, normalized via
    /// <see cref="TaskStates.Normalize"/>. Null when none were given.
    /// </summary>
    public IReadOnlyList<string>? Statuses { get; init; }

    /// <summary>
    /// The task type from a type: token, normalized via
    /// <see cref="TaskTypes.Normalize"/>.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// The priority from a p0..p4 keyword or a priority: token.
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// The assignee from an assignee: token.
    /// </summary>
    public string? Assignee { get; init; }

    /// <summary>
    /// Labels from repeated label: tokens, trimmed and lowercased. Null
    /// when none were given.
    /// </summary>
    public IReadOnlyList<string>? Labels { get; init; }

    /// <summary>
    /// The bare "ready" keyword: tasks with no unmet dependency and no
    /// future defer date.
    /// </summary>
    public bool Ready { get; init; }

    /// <summary>
    /// The bare "blocked" keyword: tasks the blocked-task computation
    /// reports as blocked. <see cref="TaskFilter"/> has no field for this
    /// (it only expresses excluding blocked tasks, for <see cref="Ready"/>);
    /// <see cref="ToFilter"/> leaves it out, and a consumer applies it by
    /// intersecting query results with a separate blocked-task computation.
    /// </summary>
    public bool Blocked { get; init; }

    /// <summary>
    /// The bare "stale" keyword: open or in-progress tasks not updated
    /// within <see cref="DefaultStaleWindow"/>.
    /// </summary>
    public bool Stale { get; init; }

    /// <summary>
    /// Maps this query onto a <see cref="TaskFilter"/>. <paramref name="now"/>
    /// resolves the time-relative fields the <see cref="Ready"/> and
    /// <see cref="Stale"/> keywords set.
    /// </summary>
    public TaskFilter ToFilter(DateTimeOffset now)
    {
        var filter = new TaskFilter
        {
            Statuses = Statuses?.ToArray(),
            IncludeAll = Statuses is null,
            ExcludeTombstones = true,
            Type = Type,
            Priority = Priority,
            Assignee = Assignee,
            Labels = Labels?.ToArray(),
            Text = Text
        };

        if (Ready)
        {
            filter = filter with
            {
                Statuses = filter.Statuses ?? [TaskStates.Open],
                ExcludeBlocked = true,
                DeferredVisibleAt = now,
                Ordering = TaskOrdering.ReadyPick
            };
        }

        if (Stale)
        {
            filter = filter with
            {
                Statuses = filter.Statuses ?? [TaskStates.Open, TaskStates.InProgress],
                UpdatedBefore = now - DefaultStaleWindow,
                Ordering = TaskOrdering.UpdatedAtAscending
            };
        }

        return filter;
    }
}
