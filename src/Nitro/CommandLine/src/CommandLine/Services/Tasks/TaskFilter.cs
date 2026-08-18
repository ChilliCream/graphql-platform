namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The parameters for <see cref="ITaskStore.QueryTasksAsync"/>. This is the
/// backend-agnostic shape the TUI's filter query parser (perles-net-4j7.2)
/// targets; its "TaskQuery" record maps onto this type. Every filter applies
/// as AND with the others; <see cref="Labels"/> apply as AND across labels.
/// </summary>
internal sealed record TaskFilter
{
    /// <summary>
    /// Statuses a task must have one of. Null defers to
    /// <see cref="IncludeAll"/>.
    /// </summary>
    public string[]? Statuses { get; init; }

    /// <summary>
    /// When <see cref="Statuses"/> is null, true includes closed and
    /// tombstone tasks and false excludes them.
    /// </summary>
    public bool IncludeAll { get; init; }

    /// <summary>
    /// Excludes tombstone tasks regardless of <see cref="Statuses"/> or
    /// <see cref="IncludeAll"/>.
    /// </summary>
    public bool ExcludeTombstones { get; init; }

    /// <summary>
    /// A task type a task must have.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// A priority a task must have.
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// An assignee a task must have. Ignored when <see cref="Unassigned"/>
    /// is true.
    /// </summary>
    public string? Assignee { get; init; }

    /// <summary>
    /// Matches tasks with no assignee. Takes precedence over
    /// <see cref="Assignee"/>.
    /// </summary>
    public bool Unassigned { get; init; }

    /// <summary>
    /// Labels a task must all carry.
    /// </summary>
    public string[]? Labels { get; init; }

    /// <summary>
    /// Text matched against a task's title, description, design, acceptance
    /// criteria, and notes.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Includes only tasks last updated at or before this instant.
    /// </summary>
    public DateTimeOffset? UpdatedBefore { get; init; }

    /// <summary>
    /// Includes only tasks whose defer_until is null or at or before this
    /// instant.
    /// </summary>
    public DateTimeOffset? DeferredVisibleAt { get; init; }

    /// <summary>
    /// Excludes tasks the blocked-task computation reports as blocked. Applied
    /// before <see cref="Limit"/>.
    /// </summary>
    public bool ExcludeBlocked { get; init; }

    /// <summary>
    /// The maximum number of tasks to return, applied after
    /// <see cref="ExcludeBlocked"/>. Null means unlimited.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The sort order applied to the results.
    /// </summary>
    public TaskOrdering Ordering { get; init; } = TaskOrdering.PriorityCreatedId;
}
