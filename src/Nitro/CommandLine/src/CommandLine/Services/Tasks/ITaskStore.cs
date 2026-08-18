namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Backend-agnostic task store used by every task command. No member exposes
/// ADO.NET or SQLite types, so the backend can change without touching a
/// command and the interface can be mocked for the TUI.
/// </summary>
internal interface ITaskStore
{
    /// <summary>
    /// Returns the nearest workspace directory at or above the current
    /// directory, or null when no workspace exists.
    /// </summary>
    string? FindWorkspaceDirectory();

    /// <summary>
    /// Returns the tasks matching the given filter, sorted per
    /// <see cref="TaskFilter.Ordering"/>.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
        TaskFilter filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the task with the given ID, or null. Tombstones are returned;
    /// callers decide whether they count.
    /// </summary>
    Task<TaskItem?> GetTaskAsync(
        string id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the task with the given ID or throws <see cref="ExitException"/>
    /// when it does not exist or is a tombstone.
    /// </summary>
    Task<TaskItem> GetRequiredTaskAsync(
        string id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a task's labels, ordered by label.
    /// </summary>
    Task<IReadOnlyList<string>> GetLabelsAsync(
        string taskId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every label in use on a non-tombstone task, with how many
    /// tasks carry it, ordered by label.
    /// </summary>
    Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a task's comments, ordered by created_at then id.
    /// </summary>
    Task<IReadOnlyList<TaskComment>> GetCommentsAsync(
        string taskId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a task's outgoing dependencies, ordered by created_at then
    /// depends_on_id.
    /// </summary>
    Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(
        string taskId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the tasks that depend on a task, ordered by created_at then
    /// task_id.
    /// </summary>
    Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(
        string taskId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every dependency edge in the workspace.
    /// </summary>
    Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Computes the set of blocked tasks from the dependency graph. Maps a
    /// blocked task ID to its blocker descriptions ("id:reason").
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every non-tombstone epic with its direct, non-tombstone child
    /// completion counts, ordered by id.
    /// </summary>
    Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the number of non-tombstone tasks.
    /// </summary>
    Task<int> CountTasksAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns non-tombstone task counts grouped by the given dimension.
    /// </summary>
    Task<IReadOnlyList<TaskCount>> CountTasksByAsync(
        TaskCountDimension dimension,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns summary statistics for the workspace.
    /// </summary>
    Task<TaskStats> GetStatsAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a configuration value, or null when the key is not set.
    /// </summary>
    Task<string?> GetConfigAsync(
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets a configuration value, overwriting any existing value for the
    /// key.
    /// </summary>
    Task SetConfigAsync(
        string key,
        string value,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every configuration key-value pair, ordered by key.
    /// </summary>
    Task<IReadOnlyList<TaskConfigEntry>> ListConfigAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the workspace's task ID prefix.
    /// </summary>
    Task<string> GetPrefixAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a workspace database in the given directory, applies the
    /// schema, and sets the task ID prefix, atomically.
    /// </summary>
    Task InitializeWorkspaceAsync(
        string workspaceDirectory,
        string prefix,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a task, its labels, and its dependencies, and records the
    /// creation event.
    /// </summary>
    Task<TaskCreationResult> CreateTaskAsync(
        TaskCreation creation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the given field changes to a task and records the
    /// corresponding events. Throws <see cref="ExitException"/> when the task
    /// does not exist, is a tombstone, or a status guard is violated.
    /// </summary>
    Task<TaskUpdateResult> UpdateTaskAsync(
        string id,
        TaskUpdate update,
        CancellationToken cancellationToken);

    /// <summary>
    /// Closes every given task and records a closed event for each. All
    /// tasks are validated before any is written: either every task closes
    /// or none does. Throws <see cref="ExitException"/> when any task does
    /// not exist, is a tombstone, or is already closed.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids,
        string reason,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reopens a closed task and records a reopened event. Throws
    /// <see cref="ExitException"/> when the task does not exist or is not
    /// closed.
    /// </summary>
    Task<TaskItem> ReopenTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Defers a task until the given instant and records a deferred event.
    /// Throws <see cref="ExitException"/> when the task does not exist or is
    /// not open or in-progress.
    /// </summary>
    Task<TaskItem> DeferTaskAsync(
        string id,
        DateTimeOffset until,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Makes a deferred task open again and records an undeferred event.
    /// Throws <see cref="ExitException"/> when the task does not exist or is
    /// not deferred.
    /// </summary>
    Task<TaskItem> UndeferTaskAsync(
        string id,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tombstones a task and records a deleted event. Throws
    /// <see cref="ExitException"/> when the task does not exist.
    /// </summary>
    Task<TaskItem> DeleteTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Closes every epic whose non-tombstone children all closed, and records
    /// a closed event for each.
    /// </summary>
    Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a comment to a task, bumps its updated_at, and records a
    /// commented event. Throws <see cref="ExitException"/> when the task does
    /// not exist or the text is empty.
    /// </summary>
    Task<TaskComment> AddCommentAsync(
        string id,
        string text,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds one or more labels to a task, bumps its updated_at when any label
    /// is new, and records a label-added event per newly added label. Throws
    /// <see cref="ExitException"/> when the task does not exist or a label is
    /// empty.
    /// </summary>
    Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(
        string id,
        IReadOnlyList<string> labels,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a label from a task, bumps its updated_at, and records a
    /// label-removed event. Throws <see cref="ExitException"/> when the task
    /// does not exist or does not carry the label.
    /// </summary>
    Task RemoveLabelAsync(
        string id,
        string label,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a dependency between two tasks, bumps the dependent's updated_at,
    /// and records a dependency-added event. Throws
    /// <see cref="ExitException"/> when either task does not exist, they are
    /// the same task, or the dependency already exists.
    /// </summary>
    Task<TaskDependencyAddResult> AddDependencyAsync(
        string id,
        string dependsOnId,
        string type,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a dependency between two tasks, bumps the dependent's
    /// updated_at, and records a dependency-removed event. Throws
    /// <see cref="ExitException"/> when the dependency does not exist.
    /// </summary>
    Task RemoveDependencyAsync(
        string id,
        string dependsOnId,
        string actor,
        CancellationToken cancellationToken);
}
