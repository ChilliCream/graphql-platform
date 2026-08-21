namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class BlockedTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List tasks that are blocked by unfinished dependencies.

            Usage:
              nitro agent tasks blocked [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks blocked
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

        // assert
        result.AssertError(
            """
            No task workspace found. Run `nitro agent tasks init` first.
            """);
    }

    [Fact]
    public async Task NoBlockedTasks_ReturnsEmptyMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "tasks", "create", "Standalone task");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

        // assert
        result.AssertSuccess(
            """
            No blocked tasks.
            """);
    }

    [Fact]
    public async Task BlockedTask_ReturnsRowWithBlockerList()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = ExtractTaskId(await ExecuteCommandAsync("agent", "tasks", "create", "Base task"));
        var dependentId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "Dependent task", "--depends-on", baseId));

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

        // assert
        result.AssertSuccess(
            $"""
            {dependentId}  P2  task  open  Dependent task  (blocked by: {baseId}:open)

            1 task(s)
            """);
    }

    [Fact]
    public async Task ClosedBlockedTask_IsExcludedFromBlockedList()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = ExtractTaskId(await ExecuteCommandAsync("agent", "tasks", "create", "Base task"));
        var dependentId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "Dependent task", "--depends-on", baseId));
        await ExecuteCommandAsync("agent", "tasks", "close", dependentId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

        // assert
        result.AssertSuccess(
            """
            No blocked tasks.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredListWithBlockers()
    {
        // arrange
        await InitWorkspaceAsync();
        var baseId = ExtractTaskId(await ExecuteCommandAsync("agent", "tasks", "create", "Base task"));
        var dependentId = ExtractTaskId(
            await ExecuteCommandAsync("agent", "tasks", "create", "Dependent task", "--depends-on", baseId));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "id": "{{dependentId}}",
                  "priority": 2,
                  "type": "task",
                  "status": "open",
                  "title": "Dependent task",
                  "blockers": [
                    "{{baseId}}:open"
                  ]
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_NoBlockedTasks_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "tasks", "create", "Standalone task");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "blocked");

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
