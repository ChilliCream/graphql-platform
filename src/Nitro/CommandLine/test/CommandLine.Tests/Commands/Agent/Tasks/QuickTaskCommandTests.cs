namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class QuickTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "q", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Quickly create a task and print only its ID.

            Usage:
              nitro agent tasks q <title> [options]

            Arguments:
              <title>  The task title

            Options:
              --priority <priority>  The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); list/ready also accept a range like 0-1 or p0-p1
              --type <type>          The task type (task, bug, feature, epic, chore, docs, question, or custom)
              --label <label>        A label; can be used multiple times
              --actor <actor>        The actor recorded on the audit log; inferred from the current session when omitted
              --output <json>        The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help         Show help and usage information

            Example:
              nitro agent tasks q "Fix the parser"
              nitro agent tasks q "Fix the parser" --priority p1 --type bug --label api
            """);
    }

    [Fact]
    public async Task MinimalArgs_PrintsOnlyTheTaskId()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "q", "Fix the parser");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Equal("acme-n5z", result.StdOut);
        Assert.Equal(
            "open|2|task|test-agent",
            await QueryScalarAsync(
                "SELECT status || '|' || priority || '|' || task_type || '|' || created_by "
                + "FROM tasks WHERE id = 'acme-n5z'"));
    }

    [Fact]
    public async Task FullOptions_CreatesTask_WithPriorityTypeAndLabels()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "q", "Fix the parser",
            "--priority", "p1",
            "--type", "bug",
            "--label", "api",
            "--label", "parser",
            "--actor", "alice");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Equal("acme-n5z", result.StdOut);
        Assert.Equal(
            "open|1|bug|alice",
            await QueryScalarAsync(
                "SELECT status || '|' || priority || '|' || task_type || '|' || created_by "
                + "FROM tasks WHERE id = 'acme-n5z'"));
        Assert.Equal(
            "api,parser",
            await QueryScalarAsync(
                "SELECT group_concat(label, ',') FROM "
                + "(SELECT label FROM labels WHERE task_id = 'acme-n5z' ORDER BY label)"));
    }

    [Fact]
    public async Task JsonOutput_MinimalArgs_ReturnsCreatedTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "q", "Fix the parser");

        // assert
        result.AssertSuccess(
            """
            {
              "id": "acme-n5z",
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
              "blockers": []
            }
            """);
    }

    [Fact]
    public async Task InvalidPriority_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "q", "Fix the parser", "--priority", "p9");

        // assert
        result.AssertError("Invalid priority 'p9'. Use 0-4 or p0-p4 (0 = critical, 4 = backlog).");
    }

    [Fact]
    public async Task TitleTooLong_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var title = new string('a', 501);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "q", title);

        // assert
        result.AssertError("The title must be 1-500 characters.");
    }

    [Fact]
    public async Task WhitespaceOnlyTitle_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "q", "   ");

        // assert
        result.AssertError("The title must be 1-500 characters.");
    }
}
