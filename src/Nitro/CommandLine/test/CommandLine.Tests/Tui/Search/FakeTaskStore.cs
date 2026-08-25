using System.Data.Common;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Search;

/// <summary>
/// An in-memory <see cref="ITaskStore"/> exercising the surface search mode
/// consumes: the query surface (<see cref="QueryTasksAsync"/>) and the read
/// surface its embedded detail pane consumes (task by id, labels, comments,
/// dependencies both directions, blocked set). Every other member throws
/// <see cref="NotSupportedException"/>: search mode never calls them.
/// </summary>
internal sealed class FakeTaskStore : ITaskStore
{
    public List<TaskItem> Tasks { get; } = [];

    /// <summary>
    /// The filter passed to the most recent <see cref="QueryTasksAsync"/>
    /// call, or null when the store has not been queried yet.
    /// </summary>
    public TaskFilter? LastFilter { get; private set; }

    /// <summary>
    /// The number of times <see cref="QueryTasksAsync"/> has been called.
    /// </summary>
    public int QueryCount { get; private set; }

    public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
        TaskFilter filter,
        CancellationToken cancellationToken)
    {
        LastFilter = filter;
        QueryCount++;

        IEnumerable<TaskItem> query = Tasks;

        if (filter.Statuses is { Length: > 0 } statuses)
        {
            query = query.Where(t => statuses.Contains(t.Status));
        }
        else if (!filter.IncludeAll)
        {
            query = query.Where(t => t.Status != TaskStates.Tombstone && t.Status != TaskStates.Closed);
        }

        if (filter.ExcludeTombstones)
        {
            query = query.Where(t => t.Status != TaskStates.Tombstone);
        }

        if (filter.Type is { } type)
        {
            query = query.Where(t => t.Type == type);
        }

        if (filter.Priority is { } priority)
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (filter.Assignee is { } assignee)
        {
            query = query.Where(t => t.Assignee == assignee);
        }

        if (filter.Text is { Length: > 0 } text)
        {
            query = query.Where(t => t.Title.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<TaskItem>>(query.ToList());
    }

    public Task<SqliteConnection> InitializeAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public string? FindWorkspaceDirectory()
        => throw new NotSupportedException();

    public Task<string?> GetConfigAsync(
        SqliteConnection connection, string key, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task SetConfigAsync(
        SqliteConnection connection, string key, string value, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task<string> GetPrefixAsync(
        SqliteConnection connection, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task<string> CreateTaskIdAsync(
        SqliteConnection connection, string? parentId, string seed, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task RecordEventAsync(
        SqliteConnection connection, TaskEvent taskEvent, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task<TaskItem?> GetTaskAsync(
        SqliteConnection connection, string id, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task<TaskItem> GetRequiredTaskAsync(
        SqliteConnection connection, string id, CancellationToken cancellationToken, DbTransaction? transaction = null)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        SqliteConnection connection, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<TaskItem?> GetTaskAsync(string id, CancellationToken cancellationToken)
        => Task.FromResult(Tasks.FirstOrDefault(t => t.Id == id));

    public Task<TaskItem> GetRequiredTaskAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<string>> GetLabelsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<TaskComment>> GetCommentsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskComment>>([]);

    public Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskDependencyDetail>>([]);

    public Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(string taskId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TaskDependentDetail>>([]);

    public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>>(
            new Dictionary<string, IReadOnlyList<string>>());

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

    public Task<TaskImportResult> ImportTasksAsync(IReadOnlyList<TaskSyncRecord> records, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task EnsureWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
