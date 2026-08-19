
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CountTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Count tasks.

            Usage:
              nitro agent tasks count [options]

            Options:
              --by <by>        Group counts by: status, type, priority, assignee, or label
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks count
              nitro agent tasks count --by status
            """);
    }

    [Fact]
    public async Task NoOptions_PrintsBareTotal_ExcludingTombstones()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        var closedId = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("agent", "tasks", "close", closedId);
        var deletedId = await CreateTaskAsync("Old task");
        await ExecuteCommandAsync("agent", "tasks", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count");

        // assert
        result.AssertSuccess("2");
    }

    [Fact]
    public async Task ByStatus_GroupsAndSortsByStatus()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        var closedId = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("agent", "tasks", "close", closedId);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "status");

        // assert
        result.AssertSuccess(
            """
            closed  1
            open  1
            """);
    }

    [Fact]
    public async Task ByPriority_FormatsPriorityLabel()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser", "--priority", "p1");
        await CreateTaskAsync("Write the docs", "--priority", "p1");
        await CreateTaskAsync("Old task", "--priority", "p3");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "priority");

        // assert
        result.AssertSuccess(
            """
            P1  2
            P3  1
            """);
    }

    [Fact]
    public async Task ByAssignee_UnassignedFallback()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser", "--assignee", "alice");
        await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "assignee");

        // assert
        result.AssertSuccess(
            """
            alice  1
            unassigned  1
            """);
    }

    [Fact]
    public async Task ByLabel_TaskCountsUnderEachLabel()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser", "--label", "api", "--label", "parser");
        await CreateTaskAsync("Write the docs", "--label", "api");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "label");

        // assert
        result.AssertSuccess(
            """
            api  2
            parser  1
            """);
    }

    [Fact]
    public async Task JsonOutput_NoOptions_ReturnsStructuredTotal()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count");

        // assert
        result.AssertSuccess(
            """
            {
              "total": 1
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_ByStatus_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "status");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "value": "open",
                  "count": 1
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task InvalidBy_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "count", "--by", "bogus");

        // assert
        result.AssertError(
            "Invalid --by value 'bogus'. Use status, type, priority, assignee, or label.");
    }
}
