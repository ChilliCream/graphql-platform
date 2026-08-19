namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CreateTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "create", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a task.

            Usage:
              nitro task create <title> [options]

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
              -?, -h, --help               Show help and usage information

            Example:
              nitro task create "Fix the parser"
              nitro task create "Fix the parser" --priority p1 --type bug --depends-on "acme-9z8"
            """);
    }

    [Fact]
    public async Task MinimalArgs_CreatesTask_WithDefaults()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "create", "Fix the parser");

        // assert
        result.AssertSuccess("✓ Created task 'acme-n5z': Fix the parser.");
        Assert.Equal(
            "open|2|task|test-agent",
            await QueryScalarAsync(
                "SELECT status || '|' || priority || '|' || task_type || '|' || created_by "
                + "FROM tasks WHERE id = 'acme-n5z'"));
    }

    [Fact]
    public async Task FullOptions_CreatesChildTask_WithDependencyAndLabels()
    {
        // arrange
        await InitWorkspaceAsync();

        var blocker = await ExecuteCommandAsync("task", "create", "Write the tokenizer tests");
        blocker.AssertSuccess();
        var parent = await ExecuteCommandAsync("task", "create", "Rewrite the parser");
        parent.AssertSuccess();

        // act
        var result = await ExecuteCommandAsync(
            "task", "create", "Fix the parser",
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
            "task", "create", "Fix the parser", "--parent", "acme-999");

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
            "task", "create", "Fix the parser", "--depends-on", "acme-999");

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
            "task", "create", "Fix the parser", "--priority", "p9");

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
        var result = await ExecuteCommandAsync("task", "create", title);

        // assert
        result.AssertError("The title must be 1-500 characters.");
    }
}
