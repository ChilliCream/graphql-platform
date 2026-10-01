namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Filters for task queries, combined with AND; a task must match every supplied label.
/// </summary>
internal sealed record TaskFilter
{
    /// <summary>
    /// Allowed normalized statuses, with null or empty deferring to
    /// <see cref="IncludeAll"/> and <see cref="IncludeArchived"/>.
    /// <see cref="ExcludeTombstones"/> still applies to explicit statuses.
    /// </summary>
    public string[]? Statuses { get; init; }

    /// <summary>
    /// Allows closed and tombstoned tasks when no explicit statuses are supplied.
    /// Archived tasks still require <see cref="IncludeArchived"/>, and
    /// <see cref="ExcludeTombstones"/> can exclude tombstones.
    /// </summary>
    public bool IncludeAll { get; init; }

    /// <summary>
    /// Allows archived tasks when <see cref="Statuses"/> is null or empty.
    /// A nonempty status filter takes precedence.
    /// </summary>
    public bool IncludeArchived { get; init; }

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
    /// The inclusive low bound of a priority range a task's priority must
    /// fall within. Ignored unless <see cref="PriorityMax"/> is also set.
    /// </summary>
    public int? PriorityMin { get; init; }

    /// <summary>
    /// The inclusive high bound of a priority range a task's priority must
    /// fall within. Ignored unless <see cref="PriorityMin"/> is also set.
    /// </summary>
    public int? PriorityMax { get; init; }

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
    /// criteria, notes, and comment text. A task matching on several
    /// comments still appears once.
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
