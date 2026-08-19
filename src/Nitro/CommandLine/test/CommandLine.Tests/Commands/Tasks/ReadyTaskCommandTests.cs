namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ReadyTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List tasks that are ready to work on.

            Usage:
              nitro agent tasks ready [options]

            Options:
              --priority <priority>  The task priority, 0-4 or p0-p4 (0 = critical, 4 = backlog); list/ready also accept a range like 0-1 or p0-p1
              --assignee <assignee>  The assignee
              --label <label>        A label; can be used multiple times
              --limit <limit>        The maximum number of tasks to show
              --include-deferred     Include deferred tasks
              --output <json>        The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help         Show help and usage information

            Example:
              nitro agent tasks ready
              nitro agent tasks ready --assignee alice --limit 5
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

        // assert
        result.AssertError(
            """
            No task workspace found. Run `nitro agent tasks init` first.
            """);
    }

    [Fact]
    public async Task EmptyWorkspace_ReturnsNoReadyTasksMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

        // assert
        result.AssertSuccess(
            """
            No ready tasks.
            """);
    }

    [Fact]
    public async Task OpenTasks_ReturnsRowsSortedByPriorityThenCreatedAt()
    {
        // arrange
        await InitWorkspaceAsync();
        var lowPriorityId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "Low priority task", "--priority", "3"));
        var highPriorityId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "High priority task", "--priority", "1"));

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

        // assert
        result.AssertSuccess(
            $"""
            {highPriorityId}  P1  task  open  High priority task
            {lowPriorityId}  P3  task  open  Low priority task

            2 task(s)
            """);
    }

    [Fact]
    public async Task BlockedTask_IsExcludedFromReadyList()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = ExtractTaskId(await ExecuteCommandAsync("agent", "tasks", "create", "Base task"));
        await ExecuteCommandAsync("agent", "tasks", "create", "Dependent task", "--depends-on", baseId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

        // assert
        result.AssertSuccess(
            $"""
            {baseId}  P2  task  open  Base task

            1 task(s)
            """);
    }

    [Fact]
    public async Task DeferredTask_ExcludedUnlessIncludeDeferred()
    {
        // arrange
        await InitWorkspaceAsync();
        var deferredId = ExtractTaskId(
            await ExecuteCommandAsync(
                "agent", "tasks", "create", "Deferred task", "--defer-until", "2026-06-01T00:00:00Z"));

        // act
        var defaultResult = await ExecuteCommandAsync("agent", "tasks", "ready");
        var includeDeferredResult = await ExecuteCommandAsync(
            "agent", "tasks", "ready", "--include-deferred");

        // assert
        defaultResult.AssertSuccess(
            """
            No ready tasks.
            """);
        includeDeferredResult.AssertSuccess(
            $"""
            {deferredId}  P2  task  open  Deferred task

            1 task(s)
            """);
    }

    [Fact]
    public async Task PriorityRange_ReturnsTasksWithinBounds()
    {
        // arrange
        await InitWorkspaceAsync();
        var criticalId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "Critical task", "--priority", "p0"));
        var highId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "High task", "--priority", "p1"));
        await ExecuteCommandAsync("agent", "tasks", "create", "Medium task", "--priority", "p2");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready", "--priority", "p0-p1");

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
        var result = await ExecuteCommandAsync("agent", "tasks", "ready", "--priority", "3-1");

        // assert
        result.AssertError(
            "Invalid priority range '3-1'. The low bound must be <= the high bound.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

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
        var result = await ExecuteCommandAsync("agent", "tasks", "ready");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    /// <summary>
    /// Pulls the task ID out of a `task create` confirmation line
    /// ("Created task 'ID': Title.") so dependent commands can use it
    /// without predicting the hash-derived ID ahead of time.
    /// </summary>
    private static string ExtractTaskId(CommandResult result)
    {
        var start = result.StdOut.IndexOf('\'') + 1;
        var end = result.StdOut.IndexOf('\'', start);
        return result.StdOut[start..end];
    }
}
