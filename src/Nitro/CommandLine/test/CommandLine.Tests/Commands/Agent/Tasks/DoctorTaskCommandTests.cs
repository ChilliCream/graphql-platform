using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class DoctorTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Check the task workspace for integrity problems.

            Usage:
              nitro agent tasks doctor [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks doctor
            """);
    }

    [Fact]
    public async Task HealthyWorkspace_ReturnsSuccess()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        result.AssertSuccess(
            $"""
            Workspace: {WorkspaceDirectory}
            Prefix: acme
            Tasks: 1

            ✓ SQLite quick_check
            ✓ Dependency cycles
            ✓ Orphan dependencies
            ✓ Orphan labels
            ✓ Orphan comments
            ✓ Tombstoned parent edges
            """);
    }

    [Fact]
    public async Task BlockingCycle_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var a = await CreateTaskAsync("Task A");
        var b = await CreateTaskAsync("Task B");

        // task dep add now rejects a cycle before commit, so the edges are
        // seeded directly, as if they reached the database another way.
        await InsertDependencyAsync(a, b);
        await InsertDependencyAsync(b, a);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Dependency cycles:", result.StdOut);
        Assert.Contains($"{smaller} -> {larger} -> {smaller}", result.StdOut);
    }

    [Fact]
    public async Task OrphanDependency_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Fix the parser");
        await InsertOrphanDependencyAsync(taskId, "missing-task", TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Orphan dependencies:", result.StdOut);
        Assert.Contains($"{taskId} -> missing-task", result.StdOut);
    }

    [Fact]
    public async Task OrphanLabel_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Fix the parser", "--label", "orphan-me");
        await DeleteTaskRowAsync(taskId, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Orphan labels:", result.StdOut);
        Assert.Contains($"{taskId}: 'orphan-me'", result.StdOut);
    }

    [Fact]
    public async Task OrphanComment_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var taskId = await CreateTaskAsync("Fix the parser");
        await InsertCommentAsync(taskId, "note", TestContext.Current.CancellationToken);
        await DeleteTaskRowAsync(taskId, TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Orphan comments:", result.StdOut);
        Assert.Contains($"{taskId}: comment #", result.StdOut);
    }

    [Fact]
    public async Task TombstonedParent_ReportedAndReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var parentId = await CreateTaskAsync("Parent epic", "--type", "epic");
        var childId = await CreateTaskAsync("Child task", "--parent", parentId);
        await ExecuteCommandAsync("agent", "tasks", "delete", parentId, "--force");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL Tombstoned parent edges:", result.StdOut);
        Assert.Contains($"{childId} -> {parentId}", result.StdOut);
    }

    [Fact]
    public async Task JsonOutput_HealthyWorkspace_ReturnsStructuredReport()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "doctor");

        // assert
        Assert.Equal(0, result.ExitCode);
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;
        Assert.Equal(WorkspaceDirectory, root.GetProperty("workspacePath").GetString());
        Assert.Equal("acme", root.GetProperty("prefix").GetString());
        Assert.Equal(1, root.GetProperty("taskCount").GetInt32());
        Assert.True(root.GetProperty("quickCheckOk").GetBoolean());
        Assert.True(root.GetProperty("healthy").GetBoolean());
        Assert.Empty(root.GetProperty("cycles").EnumerateArray());
        Assert.Empty(root.GetProperty("orphanDependencies").EnumerateArray());
    }

    private async Task InsertOrphanDependencyAsync(
        string taskId, string dependsOnId, CancellationToken cancellationToken)
    {
        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at) "
            + "VALUES (@taskId, @dependsOnId, 'blocks', @createdAt)";
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@dependsOnId", dependsOnId);
        command.Parameters.AddWithValue("@createdAt", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertCommentAsync(
        string taskId, string text, CancellationToken cancellationToken)
    {
        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO comments (task_id, author, text, created_at) "
            + "VALUES (@taskId, 'test-agent', @text, @createdAt)";
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@text", text);
        command.Parameters.AddWithValue("@createdAt", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Removes a task row directly, bypassing tombstoning, so its labels and
    /// comments become orphans for the doctor checks to find.
    /// </summary>
    private async Task DeleteTaskRowAsync(string taskId, CancellationToken cancellationToken)
    {
        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using (var disableForeignKeys = connection.CreateCommand())
        {
            disableForeignKeys.CommandText = "PRAGMA foreign_keys = OFF";
            await disableForeignKeys.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tasks WHERE id = @taskId";
        command.Parameters.AddWithValue("@taskId", taskId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
