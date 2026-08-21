using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

/// <summary>
/// Exercises the backend-agnostic read surface of <see cref="TaskStore"/>
/// directly against a real SQLite workspace, seeded with raw SQL since the
/// write surface is not implemented yet. Covers the DapperAOT-sensitive
/// paths: TEXT timestamp columns, IN-array expansion, and record
/// materialization.
/// </summary>
public sealed class TaskStoreTests : IAsyncDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly TaskStore _store;

    public TaskStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-task-store-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _store = new TaskStore(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        _tempRoot.Delete(recursive: true);
    }

    [Fact]
    public async Task QueryTasksAsync_DefaultFilter_ExcludesClosedAndTombstone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 1);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Closed, priority: 0);
        await InsertTaskAsync(connection, "acme-3", status: TaskStates.Tombstone, priority: 0);

        // act
        var tasks = await _store.QueryTasksAsync(new TaskFilter(), cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-1", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_IncludeAll_ReturnsEverything()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Closed, priority: 1);

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { IncludeAll = true }, cancellationToken);

        // assert
        Assert.Equal(["acme-2", "acme-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task QueryTasksAsync_DefaultFilter_ExcludesArchived()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 1);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Archived, priority: 0);

        // act
        var tasks = await _store.QueryTasksAsync(new TaskFilter(), cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-1", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_IncludeAll_StillExcludesArchived()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Closed, priority: 1);
        await InsertTaskAsync(connection, "acme-3", status: TaskStates.Archived, priority: 0);

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { IncludeAll = true }, cancellationToken);

        // assert
        Assert.Equal(["acme-2", "acme-1"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task QueryTasksAsync_StatusClosed_ExcludesArchived()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Closed, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Archived, priority: 1);

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { Statuses = [TaskStates.Closed] }, cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-1", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_StatusArchivedExplicit_ReturnsArchivedTasks()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Closed, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Archived, priority: 1);

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { Statuses = [TaskStates.Archived] }, cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-2", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_Labels_MatchesAllGivenLabels()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertLabelAsync(connection, "acme-1", "backend");
        await InsertLabelAsync(connection, "acme-1", "urgent");
        await InsertLabelAsync(connection, "acme-2", "backend");

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { Labels = ["backend", "urgent"] }, cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-1", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_Text_MatchesAcrossTextColumns()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2, title: "Fix the parser");
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2, title: "Unrelated");

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { Text = "parser" }, cancellationToken);

        // assert
        var task = Assert.Single(tasks);
        Assert.Equal("acme-1", task.Id);
    }

    [Fact]
    public async Task QueryTasksAsync_ExcludeBlocked_FiltersInMemoryAndAppliesLimitAfterward()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-3", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-3", TaskDependencyTypes.Blocks);

        // act
        var tasks = await _store.QueryTasksAsync(
            new TaskFilter { ExcludeBlocked = true }, cancellationToken);

        // assert
        Assert.Equal(["acme-2", "acme-3"], tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task GetStatsAsync_ComputesReadyAndBlockedCounts()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-3", status: TaskStates.Closed, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);
        await InsertLabelAsync(connection, "acme-1", "backend");
        await InsertCommentAsync(connection, "acme-1", "note");

        // act
        var stats = await _store.GetStatsAsync(cancellationToken);

        // assert
        Assert.Equal(1, stats.ReadyCount);
        Assert.Equal(["acme-1"], stats.BlockedTaskStatuses.Keys);
        Assert.Equal(1, stats.LabelCount);
        Assert.Equal(1, stats.CommentCount);
    }

    [Fact]
    public async Task CountTasksByAsync_Priority_GroupsAndFormatsAsPCode()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 0);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 0);
        await InsertTaskAsync(connection, "acme-3", status: TaskStates.Open, priority: 1);

        // act
        var counts = await _store.CountTasksByAsync(TaskCountDimension.Priority, cancellationToken);

        // assert
        Assert.Equal(
            [new TaskCount("P0", 2), new TaskCount("P1", 1)],
            counts);
    }

    [Fact]
    public async Task GetEpicStatusesAsync_CountsNonTombstoneChildren()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(connection, "acme-1.1", status: TaskStates.Closed, priority: 2);
        await InsertTaskAsync(connection, "acme-1.2", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-1.1", "acme-1", TaskDependencyTypes.ParentChild);
        await InsertDependencyAsync(connection, "acme-1.2", "acme-1", TaskDependencyTypes.ParentChild);

        // act
        var epics = await _store.GetEpicStatusesAsync(cancellationToken);

        // assert
        var epic = Assert.Single(epics);
        Assert.Equal("acme-1", epic.Id);
        Assert.Equal(2, epic.Total);
        Assert.Equal(1, epic.Closed);
        Assert.False(epic.IsEligibleForClose);
    }

    [Fact]
    public async Task GetDependencyEdgesAsync_ReturnsEveryEdgeWithTimestamp()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);

        // act
        var edges = await _store.GetDependencyEdgesAsync(cancellationToken);

        // assert
        var edge = Assert.Single(edges);
        Assert.Equal("acme-1", edge.TaskId);
        Assert.Equal("acme-2", edge.DependsOnId);
        Assert.Equal(_timeProvider.GetUtcNow(), edge.CreatedAt);
    }

    [Fact]
    public async Task GetCommentsAsync_ParsesTimestampAndOrders()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertCommentAsync(connection, "acme-1", "first");
        await InsertCommentAsync(connection, "acme-1", "second");

        // act
        var comments = await _store.GetCommentsAsync("acme-1", cancellationToken);

        // assert
        Assert.Equal(["first", "second"], comments.Select(c => c.Text));
        Assert.All(comments, c => Assert.Equal(_timeProvider.GetUtcNow(), c.CreatedAt));
    }

    [Fact]
    public async Task GetDependenciesAndDependentsAsync_JoinTargetStatusAndTitle()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2, title: "Root");
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Closed, priority: 2, title: "Target");
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);

        // act
        var dependencies = await _store.GetDependenciesAsync("acme-1", cancellationToken);
        var dependents = await _store.GetDependentsAsync("acme-2", cancellationToken);

        // assert
        var dependency = Assert.Single(dependencies);
        Assert.Equal("acme-2", dependency.DependsOnId);
        Assert.Equal(TaskStates.Closed, dependency.Status);
        Assert.Equal("Target", dependency.Title);

        var dependent = Assert.Single(dependents);
        Assert.Equal("acme-1", dependent.TaskId);
        Assert.Equal(TaskStates.Open, dependent.Status);
        Assert.Equal("Root", dependent.Title);
    }

    [Fact]
    public async Task GetLabelCountsAsync_CountsAcrossNonTombstoneTasks()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Tombstone, priority: 2);
        await InsertLabelAsync(connection, "acme-1", "backend");
        await InsertLabelAsync(connection, "acme-2", "backend");

        // act
        var counts = await _store.GetLabelCountsAsync(cancellationToken);

        // assert
        Assert.Equal([new TaskLabelCount("backend", 1)], counts);
    }

    [Fact]
    public async Task GetPrefixAndListConfigAsync_ReadWorkspaceConfig()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await ExecuteAsync(
            connection, "INSERT INTO config (key, value) VALUES ('prefix', 'acme')");

        // act
        var prefix = await _store.GetPrefixAsync(cancellationToken);
        var entries = await _store.ListConfigAsync(cancellationToken);

        // assert
        Assert.Equal("acme", prefix);
        Assert.Equal([new TaskConfigEntry("prefix", "acme")], entries);
    }

    [Fact]
    public async Task CreateTaskAsync_InsertsLabelsDependenciesAndCreatedEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        var creation = new TaskCreation
        {
            Title = "New task",
            Priority = 1,
            Type = TaskTypes.Task,
            Labels = ["backend"],
            DependsOn = [new TaskDependencyRequest("acme-1", TaskDependencyTypes.Blocks)],
            Actor = "tester"
        };

        // act
        var result = await _store.CreateTaskAsync(creation, cancellationToken);

        // assert
        Assert.Equal(["acme-1"], result.BlockedBy);

        var task = await _store.GetRequiredTaskAsync(result.Id, cancellationToken);
        Assert.Equal("New task", task.Title);
        Assert.Equal(TaskStates.Open, task.Status);

        var labels = await _store.GetLabelsAsync(result.Id, cancellationToken);
        Assert.Equal(["backend"], labels);

        var dependency = Assert.Single(await _store.GetDependenciesAsync(result.Id, cancellationToken));
        Assert.Equal("acme-1", dependency.DependsOnId);

        Assert.Equal([TaskEventTypes.Created], await QueryEventTypesAsync(connection, result.Id));
    }

    [Fact]
    public async Task UpdateTaskAsync_AppliesGivenFieldsAndRecordsEvents()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(
            connection, "acme-1", status: TaskStates.Open, priority: 2, title: "Old title");

        var update = new TaskUpdate
        {
            Actor = "tester",
            Title = "New title",
            TitleGiven = true,
            Priority = 0,
            PriorityGiven = true,
            Assignee = "alice",
            AssigneeGiven = true
        };

        // act
        var result = await _store.UpdateTaskAsync("acme-1", update, cancellationToken);

        // assert
        Assert.Equal(["title"], result.ChangedFields);

        var task = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal("New title", task.Title);
        Assert.Equal(0, task.Priority);
        Assert.Equal("alice", task.Assignee);

        Assert.Equal(
            [TaskEventTypes.PriorityChanged, TaskEventTypes.AssigneeChanged, TaskEventTypes.Updated],
            await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task UpdateTaskAsync_SettingClosedStatus_Throws()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        var update = new TaskUpdate
        {
            Actor = "tester",
            Status = TaskStates.Closed,
            StatusGiven = true
        };

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.UpdateTaskAsync("acme-1", update, cancellationToken));
    }

    [Fact]
    public async Task UpdateTaskAsync_SettingArchivedStatus_Throws()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        var update = new TaskUpdate
        {
            Actor = "tester",
            Status = TaskStates.Archived,
            StatusGiven = true
        };

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.UpdateTaskAsync("acme-1", update, cancellationToken));
    }

    [Fact]
    public async Task UpdateTaskAsync_TaskIsArchived_ThrowsOnStatusChange()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Archived, priority: 2);

        var update = new TaskUpdate
        {
            Actor = "tester",
            Status = TaskStates.Open,
            StatusGiven = true
        };

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.UpdateTaskAsync("acme-1", update, cancellationToken));
    }

    [Fact]
    public async Task CloseTaskAsync_AllOrNothing_ThrowsWhenAnyIsAlreadyClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Closed, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.CloseTaskAsync(["acme-1", "acme-2"], "reason", "tester", cancellationToken));

        var task = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Open, task.Status);
    }

    [Fact]
    public async Task CloseTaskAsync_ClosesAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act
        var closed = await _store.CloseTaskAsync(["acme-1"], "done", "tester", cancellationToken);

        // assert
        Assert.Equal(TaskStates.Closed, Assert.Single(closed).Status);
        Assert.Equal([TaskEventTypes.Closed], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task ReopenTaskAsync_ThrowsWhenNotClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.ReopenTaskAsync("acme-1", "", "tester", cancellationToken));
    }

    [Fact]
    public async Task ReopenTaskAsync_ReopensAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Closed, priority: 2);

        // act
        var task = await _store.ReopenTaskAsync("acme-1", "", "tester", cancellationToken);

        // assert
        Assert.Equal(TaskStates.Open, task.Status);
        Assert.Equal([TaskEventTypes.Reopened], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task ReopenTaskAsync_ReopensArchivedTask()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Archived, priority: 2);

        // act
        var task = await _store.ReopenTaskAsync("acme-1", "", "tester", cancellationToken);

        // assert
        Assert.Equal(TaskStates.Open, task.Status);
        Assert.Equal([TaskEventTypes.Reopened], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task DeferTaskAsync_ThrowsWhenNotOpenOrInProgress()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Closed, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.DeferTaskAsync(
                "acme-1", _timeProvider.GetUtcNow().AddDays(1), "tester", cancellationToken));
    }

    [Fact]
    public async Task DeferTaskAsync_DefersAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        var until = _timeProvider.GetUtcNow().AddDays(1);

        // act
        var task = await _store.DeferTaskAsync("acme-1", until, "tester", cancellationToken);

        // assert
        Assert.Equal(TaskStates.Deferred, task.Status);
        Assert.Equal(until, task.DeferUntil);
        Assert.Equal([TaskEventTypes.Deferred], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task UndeferTaskAsync_ThrowsWhenNotDeferred()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.UndeferTaskAsync("acme-1", "tester", cancellationToken));
    }

    [Fact]
    public async Task DeleteTaskAsync_TombstonesAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act
        var task = await _store.DeleteTaskAsync("acme-1", "no longer needed", "tester", cancellationToken);

        // assert
        Assert.Equal(TaskStates.Tombstone, task.Status);
        Assert.Equal([TaskEventTypes.Deleted], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task CloseEligibleEpicsAsync_ClosesOnlyEpicsWithAllChildrenClosed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(connection, "acme-1.1", status: TaskStates.Closed, priority: 2);
        await InsertDependencyAsync(connection, "acme-1.1", "acme-1", TaskDependencyTypes.ParentChild);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(connection, "acme-2.1", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-2.1", "acme-2", TaskDependencyTypes.ParentChild);

        // act
        var closed = await _store.CloseEligibleEpicsAsync("tester", cancellationToken);

        // assert
        var epic = Assert.Single(closed);
        Assert.Equal("acme-1", epic.Id);
        Assert.Equal(TaskStates.Closed, epic.Status);

        var task = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Closed, task.Status);
        var untouched = await _store.GetRequiredTaskAsync("acme-2", cancellationToken);
        Assert.Equal(TaskStates.Open, untouched.Status);
    }

    [Fact]
    public async Task CloseEligibleEpicsAsync_AllChildrenClosedOrArchived_ClosesEpic()
    {
        // arrange: one child closed, one child archived; both count as
        // closed for eligibility purposes.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(connection, "acme-1.1", status: TaskStates.Closed, priority: 2);
        await InsertTaskAsync(connection, "acme-1.2", status: TaskStates.Archived, priority: 2);
        await InsertDependencyAsync(connection, "acme-1.1", "acme-1", TaskDependencyTypes.ParentChild);
        await InsertDependencyAsync(connection, "acme-1.2", "acme-1", TaskDependencyTypes.ParentChild);

        // act
        var closed = await _store.CloseEligibleEpicsAsync("tester", cancellationToken);

        // assert
        var epic = Assert.Single(closed);
        Assert.Equal("acme-1", epic.Id);
        Assert.Equal(TaskStates.Closed, epic.Status);

        var task = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Closed, task.Status);
    }

    [Fact]
    public async Task CloseEligibleEpicsAsync_ArchivedEpic_IsNotReClosed()
    {
        // arrange: an already-archived epic must not be picked up again,
        // even though all of its children are closed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Archived, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(connection, "acme-1.1", status: TaskStates.Closed, priority: 2);
        await InsertDependencyAsync(connection, "acme-1.1", "acme-1", TaskDependencyTypes.ParentChild);

        // act
        var closed = await _store.CloseEligibleEpicsAsync("tester", cancellationToken);

        // assert
        Assert.Empty(closed);

        var task = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Archived, task.Status);
    }

    [Fact]
    public async Task CloseEligibleEpicsAsync_ClosingPastCap_ArchivesOverflow()
    {
        // arrange: the cap's worth of closed tasks already exist, plus an
        // epic whose only child is closed; closing the epic itself is the
        // close that pushes the total past the cap, so the epic-close path
        // must also run ArchiveExcessClosedTasksAsync.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        var baseTime = _timeProvider.GetUtcNow().AddDays(-200);

        for (var i = 1; i <= TaskStates.ClosedTaskCap; i++)
        {
            await InsertTaskAsync(
                connection,
                $"acme-{i}",
                status: TaskStates.Closed,
                priority: 2,
                closedAt: baseTime.AddMinutes(i));
        }

        await InsertTaskAsync(connection, "acme-epic", status: TaskStates.Open, priority: 2, type: TaskTypes.Epic);
        await InsertTaskAsync(
            connection,
            "acme-epic.1",
            status: TaskStates.Closed,
            priority: 2,
            closedAt: baseTime.AddMinutes(TaskStates.ClosedTaskCap + 1));
        await InsertDependencyAsync(connection, "acme-epic.1", "acme-epic", TaskDependencyTypes.ParentChild);

        // act
        var closed = await _store.CloseEligibleEpicsAsync("tester", cancellationToken);

        // assert
        var epic = Assert.Single(closed);
        Assert.Equal("acme-epic", epic.Id);

        var oldest = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Archived, oldest.Status);
        Assert.Equal([TaskEventTypes.Archived], await QueryEventTypesAsync(connection, "acme-1"));

        var nextOldest = await _store.GetRequiredTaskAsync("acme-2", cancellationToken);
        Assert.Equal(TaskStates.Archived, nextOldest.Status);

        var epicTask = await _store.GetRequiredTaskAsync("acme-epic", cancellationToken);
        Assert.Equal(TaskStates.Closed, epicTask.Status);

        var stillClosed = await _store.QueryTasksAsync(
            new TaskFilter { Statuses = [TaskStates.Closed] }, cancellationToken);
        Assert.Equal(TaskStates.ClosedTaskCap, stillClosed.Count);
    }

    [Fact]
    public async Task CloseTaskAsync_ClosingBeyondCap_ArchivesOldestClosedTask()
    {
        // arrange: 100 already-closed tasks with strictly increasing
        // closed_at, plus one open task about to become the 101st close.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        var baseTime = _timeProvider.GetUtcNow().AddDays(-200);

        for (var i = 1; i <= TaskStates.ClosedTaskCap; i++)
        {
            await InsertTaskAsync(
                connection,
                $"acme-{i}",
                status: TaskStates.Closed,
                priority: 2,
                closedAt: baseTime.AddMinutes(i));
        }

        await InsertTaskAsync(connection, "acme-101", status: TaskStates.Open, priority: 2);

        // act
        await _store.CloseTaskAsync(["acme-101"], "done", "tester", cancellationToken);

        // assert: the oldest closed task (acme-1) archived; everything else,
        // including the newly closed task, stays closed; exactly the cap
        // remains closed.
        var oldest = await _store.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Archived, oldest.Status);
        Assert.Equal(
            [TaskEventTypes.Archived], await QueryEventTypesAsync(connection, "acme-1"));

        var nextOldest = await _store.GetRequiredTaskAsync("acme-2", cancellationToken);
        Assert.Equal(TaskStates.Closed, nextOldest.Status);

        var justClosed = await _store.GetRequiredTaskAsync("acme-101", cancellationToken);
        Assert.Equal(TaskStates.Closed, justClosed.Status);
        Assert.Equal(
            [TaskEventTypes.Closed], await QueryEventTypesAsync(connection, "acme-101"));

        var stillClosed = await _store.QueryTasksAsync(
            new TaskFilter { Statuses = [TaskStates.Closed] }, cancellationToken);
        Assert.Equal(TaskStates.ClosedTaskCap, stillClosed.Count);

        var archived = await _store.QueryTasksAsync(
            new TaskFilter { Statuses = [TaskStates.Archived] }, cancellationToken);
        var archivedTask = Assert.Single(archived);
        Assert.Equal("acme-1", archivedTask.Id);

        // the default filter and --status closed both stay archived-free
        var defaultFiltered = await _store.QueryTasksAsync(new TaskFilter(), cancellationToken);
        Assert.DoesNotContain(defaultFiltered, t => t.Id == "acme-1");

        var integrity = await _store.CheckIntegrityAsync(cancellationToken);
        Assert.True(integrity.QuickCheckOk);
        Assert.Empty(integrity.OrphanDependencies);
        Assert.Empty(integrity.OrphanLabels);
        Assert.Empty(integrity.OrphanComments);
        Assert.Empty(integrity.TombstonedParentEdges);
    }

    [Fact]
    public async Task ComputeBlockedAsync_ArchivedBlocker_StillReleasesDependents()
    {
        // arrange: acme-1 depends (blocking) on acme-2, which is archived.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Archived, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);

        // act
        var blocked = await _store.ComputeBlockedAsync(cancellationToken);

        // assert: archived is terminal, same as closed, so acme-1 is not blocked.
        Assert.DoesNotContain("acme-1", blocked.Keys);
    }

    [Fact]
    public async Task ExportImportTasksAsync_RoundTripsArchivedTask()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Archived, priority: 2);

        // act
        var records = await _store.ExportTasksAsync(cancellationToken);
        var archivedRecord = Assert.Single(records, r => r.Id == "acme-1");
        Assert.Equal(TaskStates.Archived, archivedRecord.Status);

        var freshRoot = Path.Combine(_tempRoot.FullName, "fresh-import");
        Directory.CreateDirectory(freshRoot);
        var freshStore = new TaskStore(new TestFileSystem(freshRoot), _timeProvider, new AgentDatabase());
        var freshWorkspaceDirectory = AgentWorkspace.GetDirectory(freshRoot);
        Directory.CreateDirectory(freshWorkspaceDirectory);
        await freshStore.InitializeWorkspaceAsync(freshWorkspaceDirectory, "fresh", cancellationToken);

        var importResult = await freshStore.ImportTasksAsync(records, cancellationToken);

        // assert: import applies every record, including the archived one,
        // and round-trips its status verbatim.
        Assert.Equal(records.Count, importResult.Applied);
        Assert.Equal(0, importResult.Skipped);

        var imported = await freshStore.GetRequiredTaskAsync("acme-1", cancellationToken);
        Assert.Equal(TaskStates.Archived, imported.Status);
    }

    [Fact]
    public async Task AddCommentAsync_InsertsCommentAndBumpsUpdatedAt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act
        var comment = await _store.AddCommentAsync("acme-1", "Looks good.", "tester", cancellationToken);

        // assert
        Assert.Equal("Looks good.", comment.Text);
        Assert.Equal("tester", comment.Author);

        var comments = await _store.GetCommentsAsync("acme-1", cancellationToken);
        Assert.Equal(["Looks good."], comments.Select(c => c.Text));
        Assert.Equal([TaskEventTypes.Commented], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task AddCommentAsync_ThrowsOnEmptyText()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.AddCommentAsync("acme-1", "   ", "tester", cancellationToken));
    }

    [Fact]
    public async Task AddLabelAsync_OnlyBumpsUpdatedAtWhenALabelIsNewlyAdded()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertLabelAsync(connection, "acme-1", "backend");

        // act
        var results = await _store.AddLabelAsync(
            "acme-1", ["backend", "urgent"], "tester", cancellationToken);

        // assert
        Assert.Equal(
            [new TaskLabelChange("backend", false), new TaskLabelChange("urgent", true)],
            results);
        Assert.Equal([TaskEventTypes.LabelAdded], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task RemoveLabelAsync_ThrowsWhenLabelIsAbsent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.RemoveLabelAsync("acme-1", "backend", "tester", cancellationToken));
    }

    [Fact]
    public async Task RemoveLabelAsync_RemovesAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertLabelAsync(connection, "acme-1", "backend");

        // act
        await _store.RemoveLabelAsync("acme-1", "backend", "tester", cancellationToken);

        // assert
        Assert.Empty(await _store.GetLabelsAsync("acme-1", cancellationToken));
        Assert.Equal([TaskEventTypes.LabelRemoved], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task AddDependencyAsync_ThrowsWhenDependencyAlreadyExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.AddDependencyAsync(
                "acme-1", "acme-2", TaskDependencyTypes.Blocks, "tester", cancellationToken));
    }

    [Fact]
    public async Task AddDependencyAsync_DetectsBlockingCycle_RejectsBeforeCommit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-2", "acme-1", TaskDependencyTypes.Blocks);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _store.AddDependencyAsync(
                "acme-1", "acme-2", TaskDependencyTypes.Blocks, "tester", cancellationToken));

        // assert
        Assert.Equal(
            "Adding this dependency would create a cycle: acme-1 -> acme-2 -> acme-1.",
            exception.Message);
        Assert.Empty(await _store.GetDependenciesAsync("acme-1", cancellationToken));
        Assert.Empty(await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task AddDependencyAsync_InsertsAndRecordsEventWhenNoCycle()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);

        // act
        var result = await _store.AddDependencyAsync(
            "acme-1", "acme-2", TaskDependencyTypes.Blocks, "tester", cancellationToken);

        // assert
        Assert.Null(result.Cycle);
        var dependency = Assert.Single(await _store.GetDependenciesAsync("acme-1", cancellationToken));
        Assert.Equal("acme-2", dependency.DependsOnId);
        Assert.Equal(
            [TaskEventTypes.DependencyAdded], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task RemoveDependencyAsync_ThrowsWhenDependencyDoesNotExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.RemoveDependencyAsync("acme-1", "acme-2", "tester", cancellationToken));
    }

    [Fact]
    public async Task RemoveDependencyAsync_RemovesAndRecordsEvent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await InsertTaskAsync(connection, "acme-1", status: TaskStates.Open, priority: 2);
        await InsertTaskAsync(connection, "acme-2", status: TaskStates.Open, priority: 2);
        await InsertDependencyAsync(connection, "acme-1", "acme-2", TaskDependencyTypes.Blocks);

        // act
        await _store.RemoveDependencyAsync("acme-1", "acme-2", "tester", cancellationToken);

        // assert
        Assert.Empty(await _store.GetDependenciesAsync("acme-1", cancellationToken));
        Assert.Equal(
            [TaskEventTypes.DependencyRemoved], await QueryEventTypesAsync(connection, "acme-1"));
    }

    [Fact]
    public async Task ImportTasksAsync_CrossCloneCommentIdCollision_KeepsBothComments()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();

        // Two clones each independently assigned id 1 to a comment on a
        // different task; merging their exports produces two records that
        // collide on the same comments.id.
        var records = new List<TaskSyncRecord>
        {
            NewTaskSyncRecord(
                "acme-1",
                now,
                comments: [NewTaskSyncComment(1, "from-clone-a", now)]),
            NewTaskSyncRecord(
                "acme-2",
                now,
                comments: [NewTaskSyncComment(1, "from-clone-b", now)])
        };

        // act
        var result = await _store.ImportTasksAsync(records, cancellationToken);

        // assert
        Assert.Equal(2, result.Applied);
        Assert.Equal(0, result.Skipped);

        var commentsOnAcme1 = await _store.GetCommentsAsync("acme-1", cancellationToken);
        var commentsOnAcme2 = await _store.GetCommentsAsync("acme-2", cancellationToken);

        Assert.Equal(["from-clone-a"], commentsOnAcme1.Select(c => c.Text));
        Assert.Equal(["from-clone-b"], commentsOnAcme2.Select(c => c.Text));
    }

    [Fact]
    public async Task ImportTasksAsync_ReimportingSameRecord_PreservesCommentId()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var record = NewTaskSyncRecord(
            "acme-1",
            now,
            comments: [NewTaskSyncComment(7, "note", now)]);

        await _store.ImportTasksAsync([record], cancellationToken);

        // act: re-importing the exact same record must not be treated as a
        // collision against its own previously-imported comment.
        var result = await _store.ImportTasksAsync([record], cancellationToken);

        // assert
        Assert.Equal(1, result.Applied);
        var comments = await _store.GetCommentsAsync("acme-1", cancellationToken);
        var comment = Assert.Single(comments);
        Assert.Equal("note", comment.Text);
        Assert.Equal(7, comment.Id);
    }

    private static TaskSyncRecord NewTaskSyncRecord(
        string id,
        DateTimeOffset now,
        IReadOnlyList<TaskSyncComment>? comments = null) =>
        new()
        {
            Id = id,
            Title = "Task",
            Status = TaskStates.Open,
            Type = TaskTypes.Task,
            CreatedAt = now,
            UpdatedAt = now,
            Comments = comments ?? []
        };

    private static TaskSyncComment NewTaskSyncComment(long id, string author, DateTimeOffset now) =>
        new()
        {
            Id = id,
            Author = author,
            Text = author,
            CreatedAt = now
        };

    [Fact]
    public async Task SetConfigAsync_UpsertsValue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        // act
        await _store.SetConfigAsync("prefix", "acme", cancellationToken);
        await _store.SetConfigAsync("prefix", "acme2", cancellationToken);

        // assert
        Assert.Equal("acme2", await _store.GetConfigAsync("prefix", cancellationToken));
    }

    [Fact]
    public async Task InitializeWorkspaceAsync_AppliesSchemaAndSetsPrefix()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var freshRoot = Path.Combine(_tempRoot.FullName, "fresh");
        Directory.CreateDirectory(freshRoot);
        var store = new TaskStore(new TestFileSystem(freshRoot), _timeProvider, new AgentDatabase());
        var workspaceDirectory = AgentWorkspace.GetDirectory(freshRoot);
        Directory.CreateDirectory(workspaceDirectory);

        // act
        await store.InitializeWorkspaceAsync(workspaceDirectory, "fresh", cancellationToken);

        // assert
        Assert.Equal("fresh", await store.GetPrefixAsync(cancellationToken));
    }

    /// <summary>
    /// Returns the event_type column for every audit-log row on a task,
    /// ordered by id, via plain ADO.NET so the test does not need its own
    /// Dapper.AOT-compatible call shape.
    /// </summary>
    private static async Task<List<string>> QueryEventTypesAsync(SqliteConnection connection, string taskId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT event_type FROM events WHERE task_id = @taskId ORDER BY id";
        command.Parameters.AddWithValue("@taskId", taskId);

        var types = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            types.Add(reader.GetString(0));
        }

        return types;
    }

    private async Task<SqliteConnection> SeedAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);
        Directory.CreateDirectory(workspaceDirectory);

        return await _store.InitializeAsync(workspaceDirectory, cancellationToken);
    }

    private Task InsertTaskAsync(
        SqliteConnection connection,
        string id,
        string status,
        int priority,
        string title = "Task",
        string type = TaskTypes.Task,
        DateTimeOffset? closedAt = null)
    {
        var now = _timeProvider.GetUtcNow();

        return ExecuteAsync(
            connection,
            """
            INSERT INTO tasks (id, title, status, priority, task_type, created_at, updated_at, closed_at)
            VALUES (@id, @title, @status, @priority, @type, @now, @now, @closedAt)
            """,
            ("@id", id), ("@title", title), ("@status", status),
            ("@priority", priority), ("@type", type), ("@now", now),
            ("@closedAt", closedAt.HasValue ? (object)closedAt.Value : DBNull.Value));
    }

    private Task InsertLabelAsync(SqliteConnection connection, string taskId, string label)
        => ExecuteAsync(
            connection,
            "INSERT INTO labels (task_id, label) VALUES (@taskId, @label)",
            ("@taskId", taskId), ("@label", label));

    private Task InsertDependencyAsync(
        SqliteConnection connection,
        string taskId,
        string dependsOnId,
        string type)
    {
        var now = _timeProvider.GetUtcNow();

        return ExecuteAsync(
            connection,
            """
            INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at)
            VALUES (@taskId, @dependsOnId, @type, @now)
            """,
            ("@taskId", taskId), ("@dependsOnId", dependsOnId), ("@type", type), ("@now", now));
    }

    private Task InsertCommentAsync(SqliteConnection connection, string taskId, string text)
    {
        var now = _timeProvider.GetUtcNow();

        return ExecuteAsync(
            connection,
            """
            INSERT INTO comments (task_id, author, text, created_at)
            VALUES (@taskId, 'test-agent', @text, @now)
            """,
            ("@taskId", taskId), ("@text", text), ("@now", now));
    }

    /// <summary>
    /// Runs a parameterized statement via plain ADO.NET, sidestepping
    /// Dapper.AOT's interceptor so the test project does not need its own
    /// AOT-compatible call shapes.
    /// </summary>
    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }
}
