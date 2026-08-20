
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class StaleTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List open tasks that have not been updated recently.

            Usage:
              nitro agent tasks stale [options]

            Options:
              --days <days>    The staleness threshold in days (default 30)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks stale
              nitro agent tasks stale --days 14
            """);
    }

    [Fact]
    public async Task EmptyWorkspace_PrintsNoStaleTasksMessage()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

        // assert
        result.AssertSuccess("No stale tasks.");
    }

    [Fact]
    public async Task DefaultThreshold_ListsTasksNotUpdatedInThirtyDays()
    {
        // arrange
        await InitWorkspaceAsync();
        var oldId = await CreateTaskAsync("Fix the parser");
        FakeTime.Advance(TimeSpan.FromDays(31));
        await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

        // assert
        result.AssertSuccess(
            $"""
            {oldId}  P2  task  open  Fix the parser

            1 task(s)
            """);
    }

    [Fact]
    public async Task DaysOption_OverridesDefaultThreshold()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        FakeTime.Advance(TimeSpan.FromDays(10));

        // act
        var defaultResult = await ExecuteCommandAsync("agent", "tasks", "stale");
        var customResult = await ExecuteCommandAsync("agent", "tasks", "stale", "--days", "5");

        // assert
        defaultResult.AssertSuccess("No stale tasks.");
        customResult.AssertSuccess(
            $"""
            {id}  P2  task  open  Fix the parser

            1 task(s)
            """);
    }

    [Fact]
    public async Task ClosedTask_ExcludedEvenWhenOld()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "close", id);
        FakeTime.Advance(TimeSpan.FromDays(60));

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

        // assert
        result.AssertSuccess("No stale tasks.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        FakeTime.Advance(TimeSpan.FromDays(31));
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

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
    public async Task JsonOutput_NoStaleTasks_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task MultipleStaleTasks_SortedByUpdatedAtThenId()
    {
        // arrange
        await InitWorkspaceAsync();
        var first = await CreateTaskAsync("Alpha task");
        FakeTime.Advance(TimeSpan.FromDays(1));
        var second = await CreateTaskAsync("Beta task");
        FakeTime.Advance(TimeSpan.FromDays(40));

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "stale");

        // assert
        result.AssertSuccess(
            $"""
            {first}  P2  task  open  Alpha task
            {second}  P2  task  open  Beta task

            2 task(s)
            """);
    }
}
