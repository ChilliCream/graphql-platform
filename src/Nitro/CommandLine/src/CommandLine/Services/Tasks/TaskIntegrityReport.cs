namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The result of the workspace integrity checks run by
/// <see cref="ITaskStore.CheckIntegrityAsync"/>: an SQLite quick_check,
/// dependency/label/comment rows referencing a missing task, and
/// parent-child edges whose parent is a tombstone.
/// </summary>
internal sealed record TaskIntegrityReport
{
    /// <summary>
    /// True when SQLite's quick_check reported no problems.
    /// </summary>
    public required bool QuickCheckOk { get; init; }

    /// <summary>
    /// The raw quick_check message: "ok", or a description of the first
    /// problem found.
    /// </summary>
    public required string QuickCheckMessage { get; init; }

    /// <summary>
    /// Dependency rows whose task or target task does not exist.
    /// </summary>
    public required IReadOnlyList<TaskDependencyReference> OrphanDependencies { get; init; }

    /// <summary>
    /// Label rows whose task does not exist.
    /// </summary>
    public required IReadOnlyList<TaskOrphanLabel> OrphanLabels { get; init; }

    /// <summary>
    /// Comment rows whose task does not exist.
    /// </summary>
    public required IReadOnlyList<TaskOrphanComment> OrphanComments { get; init; }

    /// <summary>
    /// Parent-child dependency edges whose parent task is a tombstone.
    /// </summary>
    public required IReadOnlyList<TaskDependencyReference> TombstonedParentEdges { get; init; }
}
