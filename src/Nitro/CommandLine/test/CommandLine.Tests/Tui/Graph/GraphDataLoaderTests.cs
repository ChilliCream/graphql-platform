using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphDataLoaderTests
{
    [Fact]
    public async Task LoadAsync_Should_LoadArchivedTasksAndApplyTheClosedVisibilityOption()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(CreateTask("open", TaskStates.Open));
        store.Tasks.Add(CreateTask("archived", TaskStates.Archived));
        var loader = new GraphDataLoader(store);

        // act
        var raw = await loader.LoadAsync(TestContext.Current.CancellationToken);
        var defaultReduced = GraphReducer.Reduce(raw);
        var visibleReduced = GraphReducer.Reduce(raw, new GraphReductionOptions { HideClosed = false });

        // assert
        Assert.Equal(["archived", "open"], raw.Nodes.Select(t => t.Id));
        Assert.Equal(["open"], defaultReduced.Nodes.Select(t => t.Id));
        Assert.Equal(["archived", "open"], visibleReduced.Nodes.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadAsync_Should_ReadAllLabelsInOneBulkOperation_When_GraphLoads()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(CreateTask("one", TaskStates.Open));
        store.Tasks.Add(CreateTask("two", TaskStates.Open));
        store.TaskLabels.Add(new TaskLabels("one", ["alpha"]));
        var loader = new GraphDataLoader(store);

        // act
        var model = await loader.LoadAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, store.BulkLabelReadCount);
        Assert.Equal(0, store.SingleLabelReadCount);
        Assert.Equal(["alpha"], model.Nodes.Single(t => t.Id == "one").Labels);
    }

    private static TaskItem CreateTask(string id, string status)
        => new()
        {
            Id = id,
            Title = id,
            Status = status,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

    private sealed class FakeTaskStore : ITaskStore
    {
        public List<TaskItem> Tasks { get; } = [];
        public List<TaskLabels> TaskLabels { get; } = [];
        public int BulkLabelReadCount { get; private set; }
        public int SingleLabelReadCount { get; private set; }

        public Task<IReadOnlyList<TaskItem>> QueryTasksAsync(
            TaskFilter filter,
            CancellationToken cancellationToken)
        {
            var tasks = Tasks.Where(t =>
                (!filter.ExcludeTombstones || t.Status != TaskStates.Tombstone)
                && (t.Status != TaskStates.Archived || filter.IncludeArchived))
                .ToArray();

            return Task.FromResult<IReadOnlyList<TaskItem>>(tasks);
        }

        public Task<IReadOnlyList<TaskDependency>> GetDependencyEdgesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<TaskDependency>>([]);

        public Task<IReadOnlyList<string>> GetLabelsAsync(string taskId, CancellationToken cancellationToken)
        {
            SingleLabelReadCount++;
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<TaskLabels>> GetTaskLabelsAsync(CancellationToken cancellationToken)
        {
            BulkLabelReadCount++;
            return Task.FromResult<IReadOnlyList<TaskLabels>>(TaskLabels);
        }

        public string? FindWorkspaceDirectory() => throw new NotSupportedException();
        public Task<TaskItem?> GetTaskAsync(string id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem> GetRequiredTaskAsync(string id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskLabelCount>> GetLabelCountsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskComment>> GetCommentsAsync(string taskId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskDependencyDetail>> GetDependenciesAsync(string taskId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskDependentDetail>> GetDependentsAsync(string taskId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskEpicStatus>> GetEpicStatusesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> CountTasksAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskCount>> CountTasksByAsync(TaskCountDimension dimension, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskStats> GetStatsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string?> GetConfigAsync(string key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SetConfigAsync(string key, string value, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskConfigEntry>> ListConfigAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> GetPrefixAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InitializeWorkspaceAsync(string workspaceDirectory, string prefix, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskCreationResult> CreateTaskAsync(TaskCreation creation, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskUpdateResult> UpdateTaskAsync(string id, TaskUpdate update, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskItem>> CloseTaskAsync(IReadOnlyList<string> ids, string reason, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem> ReopenTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem> DeferTaskAsync(string id, DateTimeOffset until, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem> UndeferTaskAsync(string id, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskItem> DeleteTaskAsync(string id, string reason, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskEpicStatus>> CloseEligibleEpicsAsync(string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskComment> AddCommentAsync(string id, string text, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskLabelChange>> AddLabelAsync(string id, IReadOnlyList<string> labels, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RemoveLabelAsync(string id, string label, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TaskDependencyAddResult> AddDependencyAsync(string id, string dependsOnId, string type, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RemoveDependencyAsync(string id, string dependsOnId, string actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnsureWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
