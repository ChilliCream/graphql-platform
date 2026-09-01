namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The summary statistics returned by <see cref="ITaskStore.GetStatsAsync"/>.
/// </summary>
internal sealed record TaskStats
{
    /// <summary>
    /// Non-tombstone task counts grouped by status.
    /// </summary>
    public required IReadOnlyList<TaskCount> StatusCounts { get; init; }

    /// <summary>
    /// The number of open, non-deferred, unblocked tasks.
    /// </summary>
    public required int ReadyCount { get; init; }

    /// <summary>
    /// The blocked, non-terminal tasks, mapping task ID to status.
    /// </summary>
    public required IReadOnlyDictionary<string, string> BlockedTaskStatuses { get; init; }

    /// <summary>
    /// The number of distinct labels in use on non-tombstone tasks.
    /// </summary>
    public required int LabelCount { get; init; }

    public required int CommentCount { get; init; }
    public required int EventCount { get; init; }
}
