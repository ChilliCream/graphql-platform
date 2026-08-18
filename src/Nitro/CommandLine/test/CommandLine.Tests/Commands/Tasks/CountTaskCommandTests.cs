
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CountTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "count", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Count tasks.

            Usage:
              nitro task count [options]

            Options:
              --by <by>       Group counts by: status, type, priority, assignee, or label
              -?, -h, --help  Show help and usage information

            Example:
              nitro task count
              nitro task count --by status
            """);
    }

    [Fact]
    public async Task NoOptions_PrintsBareTotal_ExcludingTombstones()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        var closedId = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("task", "close", closedId);
        var deletedId = await CreateTaskAsync("Old task");
        await ExecuteCommandAsync("task", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("task", "count");

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
        await ExecuteCommandAsync("task", "close", closedId);

        // act
        var result = await ExecuteCommandAsync("task", "count", "--by", "status");

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
        var result = await ExecuteCommandAsync("task", "count", "--by", "priority");

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
        var result = await ExecuteCommandAsync("task", "count", "--by", "assignee");

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
        var result = await ExecuteCommandAsync("task", "count", "--by", "label");

        // assert
        result.AssertSuccess(
            """
            api  2
            parser  1
            """);
    }

    [Fact]
    public async Task InvalidBy_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "count", "--by", "bogus");

        // assert
        result.AssertError(
            "Invalid --by value 'bogus'. Use status, type, priority, assignee, or label.");
    }
}
