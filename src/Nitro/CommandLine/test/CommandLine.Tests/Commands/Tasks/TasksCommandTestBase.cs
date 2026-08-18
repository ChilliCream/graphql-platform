using ChilliCream.Nitro.CommandLine.Services.Tasks;
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
        => TaskWorkspace.GetDirectory(WorkingDirectory);

    protected string DatabasePath
        => TaskWorkspace.GetDatabasePath(WorkspaceDirectory);

    protected async Task InitWorkspaceAsync()
    {
        var result = await ExecuteCommandAsync("task", "init");
        Assert.Equal(0, result.ExitCode);
    }

    /// <summary>
    /// Creates a task via the create command, passing the given arguments
    /// after "task create", and returns the generated task ID.
    /// </summary>
    protected async Task<string> CreateTaskAsync(params string[] args)
    {
        var result = await ExecuteCommandAsync(["task", "create", .. args]);
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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _tempRoot.Delete(recursive: true);
    }
}
