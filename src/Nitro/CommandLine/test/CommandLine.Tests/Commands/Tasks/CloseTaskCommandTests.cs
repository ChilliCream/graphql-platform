
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CloseTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "close", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Close one or more tasks.

            Usage:
              nitro task close <ids>... [options]

            Arguments:
              <ids>  One or more task IDs

            Options:
              --reason <reason>  The reason recorded for this change
              --actor <actor>    The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              -?, -h, --help     Show help and usage information

            Example:
              nitro task close "app-1a2"
              nitro task close "app-1a2" "app-9z8" --reason "Fixed in v2"
            """);
    }

    [Fact]
    public async Task SingleTask_ClosesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "close", id);

        // assert
        result.AssertSuccess($"✓ Closed task '{id}'.");
        Assert.Equal("closed", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task MultipleTasks_ClosesAllTasks()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync("task", "close", id1, id2);

        // assert
        result.AssertSuccess(
            $"""
            ✓ Closed task '{id1}'.
            ✓ Closed task '{id2}'.
            """);
    }

    [Fact]
    public async Task WithReason_RecordsCloseReason()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "close", id, "--reason", "Fixed in v2");

        // assert
        result.AssertSuccess($"✓ Closed task '{id}'.");
        Assert.Equal("Fixed in v2", await QueryScalarAsync(
            $"SELECT close_reason FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task AlreadyClosed_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "close", id);

        // act
        var result = await ExecuteCommandAsync("task", "close", id);

        // assert
        result.AssertError($"Task '{id}' is already closed.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "close", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task OneTaskInvalid_RollsBackAllChanges()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("task", "close", id2);

        // act
        var result = await ExecuteCommandAsync("task", "close", id1, id2);

        // assert
        result.AssertError($"Task '{id2}' is already closed.");
        Assert.Equal("open", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id1}'"));
    }
}
