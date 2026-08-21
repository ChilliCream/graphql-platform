using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardDataLoaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnOnlyBlockedNonTerminalTasks_When_ComputedFilterIsBlocked()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-3", status: TaskStates.Closed));
        store.Blocked["a-1"] = ["a-9:open"];
        store.Blocked["a-3"] = ["a-9:open"];
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "Blocked", ComputedFilter = ColumnComputedFilter.Blocked };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_IncludeManualStatusBlockedTask_When_NoUnfinishedDependencies()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Blocked));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "Blocked", ComputedFilter = ColumnComputedFilter.Blocked };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnTaskOnce_When_StatusBlockedAndDependencyComputedBlocked()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Blocked));
        store.Blocked["a-1"] = ["a-9:open"];
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "Blocked", ComputedFilter = ColumnComputedFilter.Blocked };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_CoverEveryNonTerminalTask_When_LoadingAllDefaultViewColumns()
    {
        // arrange
        var store = new FakeTaskStore();
        var statuses = new[]
        {
            TaskStates.Open,
            TaskStates.InProgress,
            TaskStates.Blocked,
            TaskStates.Deferred,
            TaskStates.Closed,
            TaskStates.Tombstone
        };

        foreach (var status in statuses)
        {
            store.Tasks.Add(TaskItemBuilder.Create($"{status}-nodep", status: status));

            var dependencyBlockedId = $"{status}-dep";
            store.Tasks.Add(TaskItemBuilder.Create(dependencyBlockedId, status: status));
            store.Blocked[dependencyBlockedId] = ["dep-1:open"];
        }

        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));

        // act
        var visible = new HashSet<string>();
        foreach (var column in BoardView.Default.Columns)
        {
            var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);
            foreach (var task in tasks)
            {
                visible.Add(task.Id);
            }
        }

        // assert
        var nonTerminalIds = store.Tasks
            .Where(t => !TaskStates.IsTerminal(t.Status))
            .Select(t => t.Id)
            .ToList();
        Assert.All(nonTerminalIds, id => Assert.Contains(id, visible));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnDeferredStatusOrFutureDeferUntil_When_ComputedFilterIsDeferred()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Deferred));
        store.Tasks.Add(
            TaskItemBuilder.Create("a-2", status: TaskStates.Open, deferUntil: Now.AddDays(1)));
        store.Tasks.Add(
            TaskItemBuilder.Create("a-3", status: TaskStates.Open, deferUntil: Now.AddDays(-1)));
        store.Tasks.Add(TaskItemBuilder.Create("a-4", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-5", status: TaskStates.Closed));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "Deferred", ComputedFilter = ColumnComputedFilter.Deferred };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1", "a-2"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnUnblockedNonDeferredOpenTasks_When_ComputedFilterIsReady()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-3", status: TaskStates.InProgress));
        store.Blocked["a-2"] = ["a-9:open"];
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition
        {
            Name = "Ready",
            Statuses = [TaskStates.Open],
            ComputedFilter = ColumnComputedFilter.Ready
        };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnOnlyMatchingStatus_When_ColumnDeclaresStatuses()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.InProgress));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "In Progress", Statuses = [TaskStates.InProgress] };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_SortRecentFirstAndCap_When_ColumnUsesRecentFirstSortAndLimit()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Closed, closedAt: Now.AddDays(-3)));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Closed, closedAt: Now.AddDays(-1)));
        store.Tasks.Add(TaskItemBuilder.Create("a-3", status: TaskStates.Closed, closedAt: Now.AddDays(-2)));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition
        {
            Name = "Closed",
            Statuses = [TaskStates.Closed],
            Sort = BoardColumnSort.RecentFirst,
            Limit = 2
        };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-2", "a-3"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_SortByPriorityThenCreatedThenId_When_SortIsDefault()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create(
            "b-1", priority: TaskPriorities.Medium, createdAt: Now));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-1", priority: TaskPriorities.Medium, createdAt: Now));
        store.Tasks.Add(TaskItemBuilder.Create(
            "z-1", priority: TaskPriorities.High, createdAt: Now.AddDays(1)));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "All" };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["z-1", "a-1", "b-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task LoadColumnAsync_Should_ReturnEmptyList_When_NoTaskMatches()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition { Name = "In Progress", Statuses = [TaskStates.InProgress] };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task LoadColumnAsync_Should_FilterByTypesAndPriorities_When_ColumnDeclaresThem()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-1", priority: TaskPriorities.High, type: TaskTypes.Bug));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-2", priority: TaskPriorities.High, type: TaskTypes.Task));
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-3", priority: TaskPriorities.Low, type: TaskTypes.Bug));
        var loader = new BoardDataLoader(store, new FakeTimeProvider(Now));
        var column = new ColumnDefinition
        {
            Name = "Bugs",
            Types = [TaskTypes.Bug],
            Priorities = [TaskPriorities.High]
        };

        // act
        var tasks = await loader.LoadColumnAsync(column, CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], tasks.Select(t => t.Id));
    }
}
