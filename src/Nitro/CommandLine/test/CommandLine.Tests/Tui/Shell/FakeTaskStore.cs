using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// An in-memory <see cref="ITaskStore"/> exercising the surface
/// <see cref="TuiShell"/>'s cross-mode gestures consume (task by id, labels,
/// close, reopen, delete, update, create), plus the task detail surface
/// <c>BoardDetailMode</c> consumes (dependencies, blocks, comments, computed
/// blocked-by), which return empty. Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeTaskStore : ITaskStore
{
    /// <summary>
    /// When set, the next write call throws this instead of returning a
    /// result.
    /// </summary>
    public ExitException? ThrowOnWrite { get; set; }

    public Dictionary<string, TaskItem> Tasks { get; } = [];

    public Dictionary<string, string[]> Labels { get; } = [];

    public IReadOnlyList<string>? ClosedIds { get; private set; }

    public string? ReopenedId { get; private set; }

    public string? DeletedId { get; private set; }

    public string? UpdatedId { get; private set; }

    public TaskUpdate? UpdateReceived { get; private set; }

    public string? Actor { get; private set; }

    public TaskCreation? CreationReceived { get; private set; }

    public TaskCreationResult CreationResult { get; set; } = new() { Id = "a1" };

    public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(TaskFilter filter, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskItem>>([.. Tasks.Values]);

    public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskDependency>>([]);

    public Task<TaskItem?> GetTaskAsync(string id, CancellationToken cancellationToken)
        => Task.FromResult(Tasks.GetValueOrDefault(id));

    public Task<IReadOnlyList<string>> GetLabelsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(Labels.GetValueOrDefault(taskId) ?? []);

    public Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids, string reason, string actor, CancellationToken cancellationToken)
    {
        ClosedIds = ids;
        Actor = actor;
        var task = Tasks[ids[0]];
        task.Status = TaskStates.Closed;
        return Task.FromResult<IReadOnlyList<TaskItem>>([task]);
    }

    public Task<TaskItem> ReopenTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken)
    {
        ReopenedId = id;
        Actor = actor;
        var task = Tasks[id];
        task.Status = TaskStates.Open;
        return Task.FromResult(task);
    }

    public Task<TaskItem> DeleteTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken)
    {
        DeletedId = id;
        Actor = actor;
        var task = Tasks[id];
        task.Status = TaskStates.Tombstone;
        return Task.FromResult(task);
    }

    public Task<TaskUpdateResult> UpdateTaskAsync(string id, TaskUpdate update, CancellationToken cancellationToken)
    {
        UpdatedId = id;
        UpdateReceived = update;
        Actor = update.Actor;

        if (ThrowOnWrite is { } exception)
        {
            throw exception;
        }

        var changedFields = new List<string>();
        if (update.TitleGiven)
        {
            changedFields.Add("title");
        }

        if (update.StatusGiven)
        {
            changedFields.Add("status");
        }

        if (update.PriorityGiven)
        {
            changedFields.Add("priority");
        }

        if (update.TypeGiven)
        {
            changedFields.Add("type");
        }

        if (update.DescriptionGiven)
        {
            changedFields.Add("description");
        }

        if (update.NotesGiven)
        {
            changedFields.Add("notes");
        }

        return Task.FromResult(new TaskUpdateResult { ChangedFields = changedFields });
    }

    public Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(
        string id, IReadOnlyList<string> labels, string actor, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskLabelChange>>(
            [.. labels.Select(label => new TaskLabelChange(label, Added: true))]);

    public Task RemoveLabelAsync(string id, string label, string actor, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public string? FindWorkspaceDirectory() => throw new NotSupportedException();

    public Task<TaskItem> GetRequiredTaskAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskComment>> GetCommentsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskComment>>([]);

    public Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskDependencyDetail>>([]);

    public Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskDependentDetail>>([]);

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(new Dictionary<string, IReadOnlyList<string>>());

    public Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int> CountTasksAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskCount>> CountTasksByAsync(TaskCountDimension dimension, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskStats> GetStatsAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<string?> GetConfigAsync(string key, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task SetConfigAsync(string key, string value, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskConfigEntry>> ListConfigAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<string> GetPrefixAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task InitializeWorkspaceAsync(string workspaceDirectory, string prefix, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskCreationResult> CreateTaskAsync(TaskCreation creation, CancellationToken cancellationToken)
    {
        CreationReceived = creation;
        Actor = creation.Actor;

        if (ThrowOnWrite is { } exception)
        {
            throw exception;
        }

        return Task.FromResult(CreationResult);
    }

    public Task<TaskItem> DeferTaskAsync(string id, DateTimeOffset until, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> UndeferTaskAsync(string id, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskComment> AddCommentAsync(string id, string text, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskDependencyAddResult> AddDependencyAsync(string id, string dependsOnId, string type, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task RemoveDependencyAsync(string id, string dependsOnId, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task EnsureWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
