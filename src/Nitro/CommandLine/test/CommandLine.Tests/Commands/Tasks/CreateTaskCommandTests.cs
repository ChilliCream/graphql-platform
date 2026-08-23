namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CreateTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "create", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a task.

            Usage:
              nitro agent tasks create <title> [options]

            Arguments:
              <title>  The task title

            Options:
              --description <description>  The task description
              --priority <priority>        The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); list/ready also accept a range like 0-1 or p0-p1
              --type <type>                The task type (task, bug, feature, epic, chore, docs, question, or custom)
              --assignee <assignee>        The assignee
              --label <label>              A label; can be used multiple times
              --due <due>                  The due date as an ISO 8601 date or timestamp
              --defer-until <defer-until>  Hide the task from ready work until this ISO 8601 date or timestamp
              --estimate <estimate>        The estimated effort in minutes
              --actor <actor>              The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --depends-on <depends-on>    A dependency as 'id' or 'type:id'; can be used multiple times
              --parent <parent>            The parent task ID; the new task becomes its child
              --output <json>              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help               Show help and usage information

            Example:
              nitro agent tasks create "Fix the parser"
              nitro agent tasks create "Fix the parser" --priority p1 --type bug --depends-on "acme-9z8"
            """);
    }

    [Fact]
    public async Task MinimalArgs_CreatesTask_WithDefaults()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");

        // assert
        result.AssertSuccess("✓ Created task 'acme-n5z': Fix the parser.");
        Assert.Equal(
            "open|2|task|test-agent",
            await QueryScalarAsync(
                "SELECT status || '|' || priority || '|' || task_type || '|' || created_by "
                + "FROM tasks WHERE id = 'acme-n5z'"));
    }

    [Fact]
    public async Task JsonOutput_MinimalArgs_ReturnsCreatedTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "create", "Fix the parser");

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
    public async Task JsonOutput_WithDependency_IncludesComputedBlockers()
    {
        // arrange
        await InitWorkspaceAsync();
        var blockerId = await CreateTaskAsync("Write the tokenizer tests");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--depends-on", blockerId);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(blockerId, root.GetProperty("blockers")[0].GetString());
    }

    [Fact]
    public async Task FullOptions_CreatesChildTask_WithDependencyAndLabels()
    {
        // arrange
        await InitWorkspaceAsync();

        var blocker = await ExecuteCommandAsync("agent", "tasks", "create", "Write the tokenizer tests");
        blocker.AssertSuccess();
        var parent = await ExecuteCommandAsync("agent", "tasks", "create", "Rewrite the parser");
        parent.AssertSuccess();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser",
            "--description", "Handle edge cases in the tokenizer",
            "--priority", "p1",
            "--type", "bug",
            "--assignee", "alice",
            "--label", "api",
            "--label", "parser",
            "--due", "2026-01-10",
            "--defer-until", "2026-01-02",
            "--estimate", "90",
            "--parent", "acme-29d",
            "--depends-on", "acme-38b");

        // assert
        result.AssertSuccess(
            """
            ✓ Created task 'acme-29d.1': Fix the parser.
              Blocked by: acme-38b
            """);
        Assert.Equal(
            "open|1|bug|alice|90|Handle edge cases in the tokenizer",
            await QueryScalarAsync(
                "SELECT status || '|' || priority || '|' || task_type || '|' || assignee || '|' || "
                + "estimated_minutes || '|' || description FROM tasks WHERE id = 'acme-29d.1'"));
        Assert.Equal(
            "api,parser",
            await QueryScalarAsync(
                "SELECT group_concat(label, ',') FROM "
                + "(SELECT label FROM labels WHERE task_id = 'acme-29d.1' ORDER BY label)"));
        Assert.Equal(
            "acme-29d:parent-child,acme-38b:blocks",
            await QueryScalarAsync(
                "SELECT group_concat(depends_on_id || ':' || dependency_type, ',') FROM "
                + "(SELECT depends_on_id, dependency_type FROM dependencies "
                + "WHERE task_id = 'acme-29d.1' ORDER BY depends_on_id)"));
    }

    [Fact]
    public async Task MissingParent_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--parent", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task MissingDependencyTarget_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--depends-on", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task InvalidPriority_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Fix the parser", "--priority", "p9");

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
        var result = await ExecuteCommandAsync("agent", "tasks", "create", title);

        // assert
        result.AssertError("The title must be 1-500 characters.");
    }

    [Fact]
    public async Task WhitespaceOnlyTitle_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "create", "   ");

        // assert
        result.AssertError("The title must be 1-500 characters.");
    }
}
