
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class CloseTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Close one or more tasks.

            Usage:
              nitro agent tasks close <ids>... [options]

            Arguments:
              <ids>  One or more task IDs

            Options:
              --reason <reason>  The reason recorded for this change
              --actor <actor>    The actor recorded on the audit log; inferred from the current session when omitted
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent tasks close "app-1a2"
              nitro agent tasks close "app-1a2" "app-9z8" --reason "Fixed in v2"
            """);
    }

    [Fact]
    public async Task SingleTask_ClosesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id);

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
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id1, id2);

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
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id, "--reason", "Fixed in v2");

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
        await ExecuteCommandAsync("agent", "tasks", "close", id);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id);

        // assert
        result.AssertError($"Task '{id}' is already closed.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsClosedTaskSnapshots()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id1, id2);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var items = document.RootElement.GetProperty("items");

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(id1, items[0].GetProperty("id").GetString());
        Assert.Equal("closed", items[0].GetProperty("status").GetString());
        Assert.Equal(id2, items[1].GetProperty("id").GetString());
    }

    [Fact]
    public async Task OneTaskInvalid_RollsBackAllChanges()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("agent", "tasks", "close", id2);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "close", id1, id2);

        // assert
        result.AssertError($"Task '{id2}' is already closed.");
        Assert.Equal("open", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id1}'"));
    }
}
