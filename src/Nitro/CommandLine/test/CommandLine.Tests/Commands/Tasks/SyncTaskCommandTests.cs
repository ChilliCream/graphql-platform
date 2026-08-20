using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class SyncTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Synchronize the task database with the committed tasks.jsonl file.

            Usage:
              nitro agent tasks sync [options]

            Options:
              --flush-only    Write the task database to tasks.jsonl
              --import-only   Load tasks.jsonl into the task database
              --status        Report whether tasks.jsonl and the task database diverge
              -?, -h, --help  Show help and usage information

            Example:
              nitro agent tasks sync --flush-only
              nitro agent tasks sync --import-only
              nitro agent tasks sync --status
            """);
    }

    [Fact]
    public async Task NoFlags_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Specify exactly one of '--flush-only', '--import-only', or '--status'.",
            result.StdErr);
    }

    [Fact]
    public async Task MultipleFlags_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only", "--status");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "Specify exactly one of '--flush-only', '--import-only', or '--status'.",
            result.StdErr);
    }

    [Fact]
    public async Task FlushOnly_NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");

        // assert
        result.AssertError("No task workspace found. Run `nitro agent tasks init` first.");
    }

    [Fact]
    public async Task ImportOnly_NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");

        // assert
        result.AssertError("No task workspace found. Run `nitro agent tasks init` first.");
    }

    [Fact]
    public async Task ImportOnly_NoJsonl_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");

        // assert
        result.AssertError(
            "No 'tasks.jsonl' found at '.nitro/tasks/tasks.jsonl'.");
    }

    [Fact]
    public async Task FlushOnly_WritesOneLinePerTaskAndConfigEntry()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");

        // assert
        result.AssertSuccess("✓ Flushed 1 task to '.nitro/tasks/tasks.jsonl'.");

        var jsonlPath = TaskWorkspace.GetJsonlPath(WorkspaceDirectory);
        Assert.True(File.Exists(jsonlPath));

        var content = await File.ReadAllTextAsync(jsonlPath, TestContext.Current.CancellationToken);
        var lines = content.TrimEnd('\n').Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"key\":\"prefix\"", lines[0]);
        Assert.Contains($"\"id\":\"{id}\"", lines[1]);
    }

    [Fact]
    public async Task RoundTrip_FlushDeleteDatabaseImport_ShowOutputIsIdentical()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Design the schema");
        var childId = await CreateTaskAsync(
            "Implement the schema", "--depends-on", parentId);
        await ExecuteCommandAsync("agent", "tasks", "label", "add", childId, "backend");
        await ExecuteCommandAsync("agent", "tasks", "comment", "add", childId, "Looks good to me.");
        var deletedId = await CreateTaskAsync("Obsolete task");
        await ExecuteCommandAsync("agent", "tasks", "delete", deletedId, "--force");

        var beforeParent = await ExecuteCommandAsync("agent", "tasks", "show", parentId);
        var beforeChild = await ExecuteCommandAsync("agent", "tasks", "show", childId);

        // act
        var flushResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");
        Assert.Equal(0, flushResult.ExitCode);

        File.Delete(DatabasePath);
        File.Delete(DatabasePath + "-wal");
        File.Delete(DatabasePath + "-shm");

        var importResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");
        Assert.Equal(0, importResult.ExitCode);

        var afterParent = await ExecuteCommandAsync("agent", "tasks", "show", parentId);
        var afterChild = await ExecuteCommandAsync("agent", "tasks", "show", childId);
        var deletedStatus = await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{deletedId}'");

        // assert
        Assert.Equal(beforeParent.StdOut, afterParent.StdOut);
        Assert.Equal(beforeChild.StdOut, afterChild.StdOut);
        Assert.Equal("tombstone", deletedStatus);
    }

    [Fact]
    public async Task RoundTrip_FlushDeleteDatabaseImport_PrefixSurvives()
    {
        // arrange
        var initResult = await ExecuteCommandAsync("agent", "tasks", "init", "--prefix", "widget");
        Assert.Equal(0, initResult.ExitCode);
        var firstId = await CreateTaskAsync("First task");
        Assert.StartsWith("widget-", firstId);

        // act
        var flushResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");
        Assert.Equal(0, flushResult.ExitCode);

        File.Delete(DatabasePath);
        File.Delete(DatabasePath + "-wal");
        File.Delete(DatabasePath + "-shm");

        var importResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");
        Assert.Equal(0, importResult.ExitCode);

        var secondId = await CreateTaskAsync("Second task");

        // assert
        Assert.StartsWith("widget-", secondId);
    }

    [Fact]
    public async Task RoundTrip_FlushDeleteDatabaseImport_NextChildGetsNextId()
    {
        // arrange
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Design the schema");
        var firstChildId = await CreateTaskAsync(
            "Implement the schema", "--parent", parentId);

        // act
        var flushResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");
        Assert.Equal(0, flushResult.ExitCode);

        File.Delete(DatabasePath);
        File.Delete(DatabasePath + "-wal");
        File.Delete(DatabasePath + "-shm");

        var importResult = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");
        Assert.Equal(0, importResult.ExitCode);

        var secondChildId = await CreateTaskAsync(
            "Write the tests", "--parent", parentId);

        // assert
        Assert.Equal($"{parentId}.1", firstChildId);
        Assert.Equal($"{parentId}.2", secondChildId);
    }

    [Fact]
    public async Task Status_InSync_ReturnsSuccess()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--status");

        // assert
        result.AssertSuccess(
            "✓ 'tasks.jsonl' is in sync with the task database (1 task).");
    }

    [Fact]
    public async Task Status_Diverged_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");
        await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--status");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("have diverged", result.StdOut);
    }

    [Fact]
    public async Task ImportOnly_OlderRecord_DoesNotOverwriteNewerTask()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Original title");
        await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");

        FakeTime.Advance(TimeSpan.FromMinutes(5));
        await ExecuteCommandAsync("agent", "tasks", "update", id, "--title", "Newer title");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "Newer title",
            await QueryScalarAsync($"SELECT title FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task ImportOnly_MissingDatabase_CreatesWorkspaceFromJsonl()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "sync", "--flush-only");
        File.Delete(DatabasePath);
        File.Delete(DatabasePath + "-wal");
        File.Delete(DatabasePath + "-shm");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "sync", "--import-only");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(DatabasePath));
        Assert.Equal(
            "Fix the parser",
            await QueryScalarAsync($"SELECT title FROM tasks WHERE id = '{id}'"));
    }
}
