namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class ListTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List tasks.

            Usage:
              nitro agent tasks list [options]

            Options:
              --status <status>      Filter by status; can be used multiple times
              --type <type>          The task type (task, bug, feature, epic, chore, docs, question, or custom)
              --priority <priority>  The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); list/ready also accept a range like 0-1 or p0-p1
              --assignee <assignee>  The assignee
              --label <label>        A label; can be used multiple times
              --limit <limit>        The maximum number of tasks to show
              --all                  Include closed and tombstoned tasks
              --output <json>        The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help         Show help and usage information

            Example:
              nitro agent tasks list
              nitro agent tasks list --status open --status in_progress
              nitro agent tasks list --assignee alice --priority p1
            """);
    }

    [Fact]
    public async Task EmptyWorkspace_PrintsNoTasksFound()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list");

        // assert
        result.AssertSuccess("No tasks found.");
    }

    [Fact]
    public async Task List_ExcludesClosedAndTombstoneByDefault()
    {
        // arrange
        await InitWorkspaceAsync();

        await ExecuteCommandAsync("agent", "tasks", "create", "Bravo task", "--priority", "p0");
        await ExecuteCommandAsync("agent", "tasks", "create", "Alpha task");

        var closedCreateResult = await ExecuteCommandAsync("agent", "tasks", "create", "Closed task");
        var closedId = ExtractTaskId(closedCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "close", closedId);

        var deletedCreateResult = await ExecuteCommandAsync("agent", "tasks", "create", "Deleted task");
        var deletedId = ExtractTaskId(deletedCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list");

        // assert
        result.AssertSuccess(
            """
            acme-ml1  P0  task  open  Bravo task
            acme-e88  P2  task  open  Alpha task

            2 task(s)
            """);
    }

    [Fact]
    public async Task StatusOption_OverridesDefaultExclusion()
    {
        // arrange
        await InitWorkspaceAsync();

        await ExecuteCommandAsync("agent", "tasks", "create", "Open task");

        var closedCreateResult = await ExecuteCommandAsync("agent", "tasks", "create", "Closed task");
        var closedId = ExtractTaskId(closedCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "close", closedId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--status", "closed");

        // assert
        result.AssertSuccess(
            """
            acme-5vj  P2  task  closed  Closed task

            1 task(s)
            """);
    }

    [Fact]
    public async Task AllOption_IncludesClosedAndTombstone()
    {
        // arrange
        await InitWorkspaceAsync();

        await ExecuteCommandAsync("agent", "tasks", "create", "Open task");

        var closedCreateResult = await ExecuteCommandAsync("agent", "tasks", "create", "Closed task");
        var closedId = ExtractTaskId(closedCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "close", closedId);

        var deletedCreateResult = await ExecuteCommandAsync("agent", "tasks", "create", "Deleted task");
        var deletedId = ExtractTaskId(deletedCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--all");

        // assert
        result.AssertSuccess(
            """
            acme-5vj  P2  task  closed  Closed task
            acme-liq  P2  task  open  Open task
            acme-ulk  P2  task  tombstone  Deleted task

            3 task(s)
            """);
    }

    [Fact]
    public async Task Filters_NarrowByTypePriorityAssigneeAndLabel()
    {
        // arrange
        await InitWorkspaceAsync();

        await ExecuteCommandAsync(
            "agent", "tasks", "create", "Target task",
            "--type", "bug", "--priority", "p1", "--assignee", "alice",
            "--label", "api", "--label", "parser");
        await ExecuteCommandAsync(
            "agent", "tasks", "create", "Missing label task",
            "--type", "bug", "--priority", "p1", "--assignee", "alice", "--label", "api");
        await ExecuteCommandAsync(
            "agent", "tasks", "create", "Different type task",
            "--type", "feature", "--priority", "p1", "--assignee", "alice",
            "--label", "api", "--label", "parser");

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "list",
            "--type", "bug", "--priority", "p1", "--assignee", "alice",
            "--label", "api", "--label", "parser");

        // assert
        result.AssertSuccess(
            """
            acme-tyi  P1  bug  open  Target task

            1 task(s)
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "id": "{{id}}",
                  "priority": 2,
                  "type": "task",
                  "status": "open",
                  "title": "Fix the parser"
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_EmptyWorkspace_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task InvalidPriorityOption_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--priority", "p9");

        // assert
        result.AssertError(
            "Invalid priority 'p9'. Use 0-4 or p0-p4 (0 = critical, 4 = backlog).");
    }

    [Fact]
    public async Task PriorityRange_ReturnsTasksWithinBounds()
    {
        // arrange
        await InitWorkspaceAsync();

        var criticalCreateResult = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Critical task", "--priority", "p0");
        var criticalId = ExtractTaskId(criticalCreateResult.StdOut);
        var highCreateResult = await ExecuteCommandAsync(
            "agent", "tasks", "create", "High task", "--priority", "p1");
        var highId = ExtractTaskId(highCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "create", "Medium task", "--priority", "p2");
        await ExecuteCommandAsync("agent", "tasks", "create", "Backlog task", "--priority", "p4");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--priority", "p0-p1");

        // assert
        result.AssertSuccess(
            $"""
            {criticalId}  P0  task  open  Critical task
            {highId}  P1  task  open  High task

            2 task(s)
            """);
    }

    [Fact]
    public async Task PriorityRange_PlainDigits_ReturnsTasksWithinBounds()
    {
        // arrange
        await InitWorkspaceAsync();

        var criticalCreateResult = await ExecuteCommandAsync(
            "agent", "tasks", "create", "Critical task", "--priority", "p0");
        var criticalId = ExtractTaskId(criticalCreateResult.StdOut);
        var highCreateResult = await ExecuteCommandAsync(
            "agent", "tasks", "create", "High task", "--priority", "p1");
        var highId = ExtractTaskId(highCreateResult.StdOut);
        await ExecuteCommandAsync("agent", "tasks", "create", "Medium task", "--priority", "p2");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--priority", "0-1");

        // assert
        result.AssertSuccess(
            $"""
            {criticalId}  P0  task  open  Critical task
            {highId}  P1  task  open  High task

            2 task(s)
            """);
    }

    [Fact]
    public async Task PriorityRange_LowGreaterThanHigh_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "list", "--priority", "3-1");

        // assert
        result.AssertError(
            "Invalid priority range '3-1'. The low bound must be <= the high bound.");
    }

    private static string ExtractTaskId(string stdout) => stdout.Split('\'')[1];
}
