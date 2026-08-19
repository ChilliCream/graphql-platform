using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ShowTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show a task's details.

            Usage:
              nitro agent tasks show <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks show "acme-1a2"
            """);
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task MinimalTask_DisplaysHeaderOnly()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", id);

        // assert
        result.AssertSuccess(
            $"""
            {id}: Fix the parser

            Status: open  Priority: P2  Type: task
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00
            """);
    }

    [Fact]
    public async Task TaskWithOptionalFields_DisplaysAllHeaderLinesAndDescription()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync(
            "Ship the release",
            "--description", "Coordinate the release checklist.",
            "--priority", "p1",
            "--type", "feature",
            "--assignee", "alice",
            "--label", "api",
            "--label", "parser",
            "--due", "2026-01-05T00:00:00Z",
            "--defer-until", "2026-01-03T00:00:00Z",
            "--estimate", "90");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", id);

        // assert
        result.AssertSuccess(
            $"""
            {id}: Ship the release

            Status: open  Priority: P1  Type: feature
            Assignee: alice
            Due: 2026-01-05 00:00
            Deferred until: 2026-01-03 00:00
            Estimate: 90m
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00
            Labels: api, parser

            Description:
              Coordinate the release checklist.
            """);
    }

    [Fact]
    public async Task ClosedTask_DisplaysClosedLineWithReason()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Investigate flaky test");
        var closeResult = await ExecuteCommandAsync(
            "agent", "tasks", "close", id, "--reason", "Fixed by retry logic");
        Assert.Equal(0, closeResult.ExitCode);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", id);

        // assert
        result.AssertSuccess(
            $"""
            {id}: Investigate flaky test

            Status: closed  Priority: P2  Type: task
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00
            Closed: 2026-01-01 00:00 (Fixed by retry logic)
            """);
    }

    [Fact]
    public async Task TaskWithDependency_DisplaysDependenciesBlocksAndBlockedBy()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = await CreateTaskAsync("Fix the parser");
        var dependentId = await CreateTaskAsync("Fix the lexer", "--depends-on", baseId);

        // act
        var dependent = await ExecuteCommandAsync("agent", "tasks", "show", dependentId);
        var dependency = await ExecuteCommandAsync("agent", "tasks", "show", baseId);

        // assert
        dependent.AssertSuccess(
            $"""
            {dependentId}: Fix the lexer

            Status: open  Priority: P2  Type: task
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00
            Blocked by: {baseId}:open

            Dependencies:
              blocks -> {baseId} (open) Fix the parser
            """);
        dependency.AssertSuccess(
            $"""
            {baseId}: Fix the parser

            Status: open  Priority: P2  Type: task
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00

            Blocks:
              {dependentId} (open) Fix the lexer
            """);
    }

    [Fact]
    public async Task TaskWithComments_DisplaysComments()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Document the API");
        await InsertCommentAsync(id, "alice", "Looks good to me.", cancellationToken);
        await InsertCommentAsync(id, "test-agent", "Thanks, merging.", cancellationToken);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", id);

        // assert
        result.AssertSuccess(
            $"""
            {id}: Document the API

            Status: open  Priority: P2  Type: task
            Created: 2026-01-01 00:00 by test-agent
            Updated: 2026-01-01 00:00

            Comments:
              [1] alice 2026-01-01 00:00
                Looks good to me.

              [2] test-agent 2026-01-01 00:00
                Thanks, merging.
            """);
    }

    [Fact]
    public async Task JsonOutput_MinimalTask_ReturnsStructuredDetail()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", id);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{id}}",
              "title": "Fix the parser",
              "status": "open",
              "priority": 2,
              "type": "task",
              "assignee": null,
              "estimatedMinutes": null,
              "dueAt": null,
              "deferUntil": null,
              "createdAt": "2026-01-01T00:00:00+00:00",
              "createdBy": "test-agent",
              "updatedAt": "2026-01-01T00:00:00+00:00",
              "closedAt": null,
              "closeReason": null,
              "description": "",
              "design": "",
              "acceptanceCriteria": "",
              "notes": "",
              "labels": [],
              "blockers": [],
              "dependencies": [],
              "dependents": [],
              "comments": []
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_TaskWithDependency_EmbedsDependenciesAndBlockers()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = await CreateTaskAsync("Fix the parser");
        var dependentId = await CreateTaskAsync("Fix the lexer", "--depends-on", baseId);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "show", dependentId);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("open", root.GetProperty("blockers")[0].GetString()!.Split(':')[1]);
        Assert.Equal(baseId, root.GetProperty("blockers")[0].GetString()!.Split(':')[0]);
        Assert.Equal("blocks", root.GetProperty("dependencies")[0].GetProperty("type").GetString());
        Assert.Equal(baseId, root.GetProperty("dependencies")[0].GetProperty("dependsOnId").GetString());
    }

    /// <summary>
    /// Inserts a comment directly, since this wave has no `task comment` command
    /// to create one through.
    /// </summary>
    private async Task InsertCommentAsync(
        string taskId, string author, string text, CancellationToken cancellationToken)
    {
        await using var connection =
            new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO comments (task_id, author, text, created_at) "
            + "VALUES (@taskId, @author, @text, @createdAt)";
        command.Parameters.AddWithValue("@taskId", taskId);
        command.Parameters.AddWithValue("@author", author);
        command.Parameters.AddWithValue("@text", text);
        command.Parameters.AddWithValue("@createdAt", FakeTime.GetUtcNow());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
