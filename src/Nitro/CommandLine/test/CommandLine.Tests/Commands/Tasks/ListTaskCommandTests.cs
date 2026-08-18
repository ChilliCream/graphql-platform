namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List tasks.

            Usage:
              nitro task list [options]

            Options:
              --status <status>      Filter by status; can be used multiple times
              --type <type>          The task type (task, bug, feature, epic, chore, docs, question, or custom)
              --priority <priority>  The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog)
              --assignee <assignee>  The assignee
              --label <label>        A label; can be used multiple times
              --limit <limit>        The maximum number of tasks to show
              --all                  Include closed and tombstoned tasks
              -?, -h, --help         Show help and usage information

            Example:
              nitro task list
              nitro task list --status open --status in_progress
              nitro task list --assignee alice --priority p1
            """);
    }

    [Fact]
    public async Task EmptyWorkspace_PrintsNoTasksFound()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "list");

        // assert
        result.AssertSuccess("No tasks found.");
    }

    [Fact]
    public async Task List_ExcludesClosedAndTombstoneByDefault()
    {
        // arrange
        await InitWorkspaceAsync();

        await ExecuteCommandAsync("task", "create", "Bravo task", "--priority", "p0");
        await ExecuteCommandAsync("task", "create", "Alpha task");

        var closedId = ExtractTaskId(
            (await ExecuteCommandAsync("task", "create", "Closed task")).StdOut);
        await ExecuteCommandAsync("task", "close", closedId);

        var deletedId = ExtractTaskId(
            (await ExecuteCommandAsync("task", "create", "Deleted task")).StdOut);
        await ExecuteCommandAsync("task", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("task", "list");

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

        await ExecuteCommandAsync("task", "create", "Open task");

        var closedId = ExtractTaskId(
            (await ExecuteCommandAsync("task", "create", "Closed task")).StdOut);
        await ExecuteCommandAsync("task", "close", closedId);

        // act
        var result = await ExecuteCommandAsync("task", "list", "--status", "closed");

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

        await ExecuteCommandAsync("task", "create", "Open task");

        var closedId = ExtractTaskId(
            (await ExecuteCommandAsync("task", "create", "Closed task")).StdOut);
        await ExecuteCommandAsync("task", "close", closedId);

        var deletedId = ExtractTaskId(
            (await ExecuteCommandAsync("task", "create", "Deleted task")).StdOut);
        await ExecuteCommandAsync("task", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("task", "list", "--all");

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
            "task", "create", "Target task",
            "--type", "bug", "--priority", "p1", "--assignee", "alice",
            "--label", "api", "--label", "parser");
        await ExecuteCommandAsync(
            "task", "create", "Missing label task",
            "--type", "bug", "--priority", "p1", "--assignee", "alice", "--label", "api");
        await ExecuteCommandAsync(
            "task", "create", "Different type task",
            "--type", "feature", "--priority", "p1", "--assignee", "alice",
            "--label", "api", "--label", "parser");

        // act
        var result = await ExecuteCommandAsync(
            "task", "list",
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
    public async Task InvalidPriorityOption_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "list", "--priority", "p9");

        // assert
        result.AssertError(
            "Invalid priority 'p9'. Use 0-4 or p0-p4 (0 = critical, 4 = backlog).");
    }

    private static string ExtractTaskId(string stdout) => stdout.Split('\'')[1];
}
