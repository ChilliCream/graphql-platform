using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

/// <summary>
/// Runs task commands against a real SQLite workspace in a per-test temp
/// directory named "acme", so the derived task ID prefix is deterministic.
/// </summary>
public abstract class TasksCommandTestBase : CommandTestBase
{
    private readonly DirectoryInfo _tempRoot;

    protected TasksCommandTestBase(NitroCommandFixture fixture) : base(fixture)
    {
        SetupNoAuthentication();
        SetupEnvironmentVariable(TaskActor.EnvironmentVariableName, "test-agent");

        _tempRoot = Directory.CreateTempSubdirectory("nitro-task-tests");
        WorkingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(WorkingDirectory);
        SetupFileSystem(new TestFileSystem(WorkingDirectory));
    }

    protected string WorkingDirectory { get; }

    protected string WorkspaceDirectory
        => AgentWorkspace.GetDirectory(WorkingDirectory);

    protected string DatabasePath
        => AgentWorkspace.GetDatabasePath(WorkspaceDirectory);

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("agent", "tasks", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Creates a task via the create command, passing the given arguments
    /// after "agent tasks create", and returns the generated task ID.
    /// </summary>
    protected async Task<string> CreateTaskAsync(params string[] args)
    {
        var result = await ExecuteCommandAsync(["agent", "tasks", "create", .. args]);
        Assert.Equal(0, result.ExitCode);

        var start = result.StdOut.IndexOf('\'') + 1;
        var end = result.StdOut.IndexOf('\'', start);
        Assert.True(end > start, $"Expected a created task ID in: {result.StdOut}");

        return result.StdOut[start..end];
    }

    /// <summary>
    /// Runs a scalar query against the workspace database and returns the
    /// first column of the first row as a string.
    /// </summary>
    protected async Task<string?> QueryScalarAsync(string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? null : result.ToString();
    }

    /// <summary>
    /// Inserts a dependency edge directly into the workspace database,
    /// bypassing ITaskStore's cycle rejection. Used to seed a cycle that
    /// reached the database some other way (a stale sync import, a manual
    /// edit) so cycle-detection commands have something to find.
    /// </summary>
    protected async Task InsertDependencyAsync(string taskId, string dependsOnId, string type = "blocks")
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at) "
            + "VALUES (@taskId, @dependsOnId, @type, @now)";
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@dependsOnId", dependsOnId);
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
