using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

/// <summary>
/// An in-memory <see cref="ITaskStore"/> exercising only the lifecycle write
/// surface <see cref="TaskLifecycleActions"/> consumes (close, reopen,
/// delete). Every other member throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeTaskStore : ITaskStore
{
    /// <summary>
    /// When set, the next call to <see cref="CloseTaskAsync"/>,
    /// <see cref="ReopenTaskAsync"/>, or <see cref="DeleteTaskAsync"/> throws
    /// this instead of returning a result.
    /// </summary>
    public ExitException? ThrowOnWrite { get; set; }

    public IReadOnlyList<string>? ClosedIds { get; private set; }

    public string? ClosedReason { get; private set; }

    public string? ReopenedId { get; private set; }

    public string? ReopenedReason { get; private set; }

    public string? DeletedId { get; private set; }

    public string? DeletedReason { get; private set; }

    public string? Actor { get; private set; }

    public TaskItem ResultTask { get; set; } = null!;

    public Task<IReadOnlyList<TaskItem>> CloseTaskAsync(
        IReadOnlyList<string> ids, string reason, string actor, CancellationToken cancellationToken)
    {
        ClosedIds = ids;
        ClosedReason = reason;
        Actor = actor;

        if (ThrowOnWrite is { } exception)
        {
            throw exception;
        }

        return Task.FromResult<IReadOnlyList<TaskItem>>([ResultTask]);
    }

    public Task<TaskItem> ReopenTaskAsync(
        string id, string reason, string actor, CancellationToken cancellationToken)
    {
        ReopenedId = id;
        ReopenedReason = reason;
        Actor = actor;

        if (ThrowOnWrite is { } exception)
        {
            throw exception;
        }

        return Task.FromResult(ResultTask);
    }

    public Task<TaskItem> DeleteTaskAsync(
        string id, string reason, string actor, CancellationToken cancellationToken)
    {
        DeletedId = id;
        DeletedReason = reason;
        Actor = actor;

        if (ThrowOnWrite is { } exception)
        {
            throw exception;
        }

        return Task.FromResult(ResultTask);
    }

    public string? FindWorkspaceDirectory() => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(TaskFilter filter, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem?> GetTaskAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> GetRequiredTaskAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> GetLabelsAsync(string taskId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskComment>> GetCommentsAsync(string taskId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(string taskId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(string taskId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

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
        => throw new NotSupportedException();

    public Task<TaskUpdateResult> UpdateTaskAsync(string id, TaskUpdate update, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> DeferTaskAsync(string id, DateTimeOffset until, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> UndeferTaskAsync(string id, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskComment> AddCommentAsync(string id, string text, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(string id, IReadOnlyList<string> labels, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task RemoveLabelAsync(string id, string label, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskDependencyAddResult> AddDependencyAsync(string id, string dependsOnId, string type, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task RemoveDependencyAsync(string id, string dependsOnId, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
