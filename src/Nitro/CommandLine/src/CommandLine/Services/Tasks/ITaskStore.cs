namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Stores tasks, dependencies, labels, comments, and their audit events.
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
    /// Returns the tasks an agent participated in: tasks with at least one event whose
    /// actor is the agent, and tasks currently assigned to the agent. Tombstones are
    /// excluded; every other status is included. Ordered by the agent's latest event on
    /// the task, falling back to the task's updated_at for a task that is only assigned,
    /// newest first then by id. A null limit returns every matching task.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> QueryParticipationAsync(
        string agent,
        int? limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the task, including a tombstone, or null when its id does not exist.
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
    /// Returns every dependency edge ordered by task id and target id.
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
    /// Initializes the workspace database and schema, then sets its task id prefix.
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
    /// Applies field changes and records events for each task, rejecting missing tasks,
    /// tombstones, and invalid updates. The default implementation updates tasks
    /// individually and can leave earlier updates committed if a later update fails.
    /// </summary>
    async Task<IReadOnlyList<TaskItem>> UpdateTasksAsync(
        IReadOnlyList<string> ids,
        TaskUpdate update,
        CancellationToken cancellationToken)
    {
        var results = new List<TaskItem>(ids.Count);

        foreach (var id in ids)
        {
            await UpdateTaskAsync(id, update, cancellationToken);
            results.Add(await GetRequiredTaskAsync(id, cancellationToken));
        }

        return results;
    }

    /// <summary>
    /// Reassigns active tasks and adds the supplied comment to each, returning their ids
    /// in ascending order. The default implementation processes tasks individually
    /// without guaranteeing an atomic batch.
    /// </summary>
    async Task<IReadOnlyList<string>> ReassignAsync(
        string from,
        string to,
        string actor,
        string comment,
        CancellationToken cancellationToken)
    {
        if (from == to)
        {
            throw new ExitException("The source and target assignees must differ.");
        }

        var tasks = await QueryTasksAsync(
            new TaskFilter { Assignee = from, IncludeAll = true, IncludeArchived = true },
            cancellationToken);
        var ids = tasks
            .Where(task => task.Status is not (
                TaskStates.Closed or TaskStates.Tombstone or TaskStates.Archived))
            .Select(task => task.Id)
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var id in ids)
        {
            await UpdateTaskAsync(
                id,
                new TaskUpdate { Actor = actor, Assignee = to, AssigneeGiven = true },
                cancellationToken);
            await AddCommentAsync(id, comment, actor, cancellationToken);
        }

        return ids;
    }

    /// <summary>
    /// Closes every given task and records a closed event for each. All
    /// tasks are validated before any is written: either every task closes
    /// or none does. Throws <see cref="ExitException"/> when any task does
    /// not exist, is a tombstone, or is already closed. After the close
    /// commits, enforces <see cref="TaskStates.ClosedTaskCap"/>: if the
    /// closed count now exceeds the cap, the oldest closed tasks move to
    /// <see cref="TaskStates.Archived"/> until exactly the cap remains.
    /// </summary>
    Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids,
        string reason,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reopens a closed or archived task and records a reopened event.
    /// Throws <see cref="ExitException"/> when the task does not exist or is
    /// not closed or archived.
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
    /// Tombstones a task and records a deleted event.
    /// Throws <see cref="ExitException"/> when the task is missing or already tombstoned.
    /// </summary>
    Task<TaskItem> DeleteTaskAsync(
        string id,
        string reason,
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases every in-progress task assigned to the given agent, moving each to open
    /// with no assignee and recording the reason. Returns the number of tasks released.
    /// </summary>
    Task<int> ReleaseAssigneeAsync(
        string agent,
        string reason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Closes every epic whose non-tombstone children all closed, and records
    /// a closed event for each. After the close commits, enforces
    /// <see cref="TaskStates.ClosedTaskCap"/> the same way
    /// <see cref="CloseTaskAsync"/> does.
    /// </summary>
    Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(
        string actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a comment, refreshes the task's modification time, and records a commented event.
    /// Throws <see cref="ExitException"/> for a missing or tombstoned task or blank text.
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
    /// the same task, the dependency already exists, or (for a blocking
    /// dependency type) adding it would close a cycle; a rejected write is
    /// rolled back before it commits.
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

    /// <summary>
    /// Replaces the task's parent, or removes it for a null or empty parent id, updating
    /// its modification time and recording edge changes; an unchanged parent is a no-op.
    /// Invalid tasks, duplicate edges, and blocking cycles are rejected, but the default
    /// implementation can leave previous parents removed when adding the new parent fails.
    /// </summary>
    async Task<TaskDependencyAddResult> SetParentAsync(
        string id,
        string? parentId,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedParentId = string.IsNullOrEmpty(parentId) ? null : parentId;

        if (normalizedParentId == id)
        {
            throw new ExitException("A task cannot be its own parent.");
        }

        await GetRequiredTaskAsync(id, cancellationToken);

        var dependencies = await GetDependenciesAsync(id, cancellationToken);
        var existingParentIds = dependencies
            .Where(dependency => dependency.Type == TaskDependencyTypes.ParentChild)
            .Select(dependency => dependency.DependsOnId)
            .ToList();

        if (existingParentIds.Count == 1 && existingParentIds[0] == normalizedParentId)
        {
            return new TaskDependencyAddResult { Cycle = null };
        }

        foreach (var existingParentId in existingParentIds)
        {
            await RemoveDependencyAsync(id, existingParentId, actor, cancellationToken);
        }

        if (normalizedParentId is not null)
        {
            return await AddDependencyAsync(
                id, normalizedParentId, TaskDependencyTypes.ParentChild, actor, cancellationToken);
        }

        return new TaskDependencyAddResult { Cycle = null };
    }

    /// <summary>
    /// Creates the workspace database and schema if the database file is absent,
    /// without setting a task id prefix. An existing database is unchanged.
    /// </summary>
    Task EnsureWorkspaceAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reports database integrity, orphaned dependencies, labels and comments, and
    /// edges to tombstoned parents. The default implementation returns an empty report
    /// without performing checks.
    /// </summary>
    Task<TaskIntegrityReport> CheckIntegrityAsync(
        CancellationToken cancellationToken)
        => Task.FromResult(new TaskIntegrityReport
        {
            QuickCheckOk = true,
            QuickCheckMessage = "ok",
            OrphanDependencies = [],
            OrphanLabels = [],
            OrphanComments = [],
            TombstonedParentEdges = []
        });
}
