using ChilliCream.Nitro.CommandLine.Services.Tasks;
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

        _store = new TaskStore(new TestFileSystem(_workingDirectory), _timeProvider);
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

    private async Task<SqliteConnection> SeedAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = TaskWorkspace.GetDirectory(_workingDirectory);
        Directory.CreateDirectory(workspaceDirectory);

        return await _store.InitializeAsync(workspaceDirectory, cancellationToken);
    }

    private Task InsertTaskAsync(
        SqliteConnection connection,
        string id,
        string status,
        int priority,
        string title = "Task",
        string type = TaskTypes.Task)
    {
        var now = _timeProvider.GetUtcNow();

        return ExecuteAsync(
            connection,
            """
            INSERT INTO tasks (id, title, status, priority, task_type, created_at, updated_at)
            VALUES (@id, @title, @status, @priority, @type, @now, @now)
            """,
            ("@id", id), ("@title", title), ("@status", status),
            ("@priority", priority), ("@type", type), ("@now", now));
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
