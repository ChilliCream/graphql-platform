using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Tree;

/// <summary>
/// An in-memory <see cref="ITaskStore"/> exercising only the graph-read
/// surface the dependency tree explorer consumes
/// (<see cref="QueryTasksAsync"/>, <see cref="GetDependencyEdgesAsync"/>).
/// Every other member throws <see cref="NotSupportedException"/>: the tree
/// explorer never calls them.
/// </summary>
internal sealed class FakeTaskStore : ITaskStore
{
    public List<TaskItem> Tasks { get; } = [];

    public List<TaskDependency> Edges { get; } = [];

    /// <summary>
    /// The number of times <see cref="GetDependencyEdgesAsync"/> has been
    /// called.
    /// </summary>
    public int EdgeQueryCount { get; private set; }

    public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskItem>>(Tasks.ToList());

    public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(
        CancellationToken cancellationToken)
    {
        EdgeQueryCount++;
        return Task.FromResult<IReadOnlyList<TaskDependency>>(Edges.ToList());
    }

    public string? FindWorkspaceDirectory()
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

    public Task<IReadOnlyList<TaskItem>> CloseTaskAsync(IReadOnlyList<string> ids, string reason, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> ReopenTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> DeferTaskAsync(string id, DateTimeOffset until, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> UndeferTaskAsync(string id, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem> DeleteTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken)
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
