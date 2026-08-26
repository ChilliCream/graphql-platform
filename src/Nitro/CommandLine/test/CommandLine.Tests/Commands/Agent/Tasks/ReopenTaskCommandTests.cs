
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class ReopenTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Reopen a closed task.

            Usage:
              nitro agent tasks reopen <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --reason <reason>  The reason recorded for this change
              --actor <actor>    The actor recorded on the audit log; inferred from the current session when omitted
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro agent tasks reopen "app-1a2"
            """);
    }

    [Fact]
    public async Task ClosedTask_ReopensSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "close", id, "--reason", "Not needed");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", id);

        // assert
        result.AssertSuccess($"✓ Reopened task '{id}'.");
        Assert.Equal("open", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
        Assert.Null(await QueryScalarAsync(
            $"SELECT closed_at FROM tasks WHERE id = '{id}'"));
        Assert.Equal("", await QueryScalarAsync(
            $"SELECT close_reason FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task WithReason_RecordsReopenReason()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "close", id);

        // act
        var result = await ExecuteCommandAsync(
            "agent", "tasks", "reopen", id, "--reason", "Needed after all");

        // assert
        result.AssertSuccess($"✓ Reopened task '{id}'.");
        Assert.Equal("Needed after all", await QueryScalarAsync(
            $"SELECT comment FROM events WHERE task_id = '{id}' AND event_type = 'reopened'"));
    }

    [Fact]
    public async Task WithActor_RecordsActorOnEvent()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "close", id);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", id, "--actor", "alice");

        // assert
        result.AssertSuccess($"✓ Reopened task '{id}'.");
        Assert.Equal("alice", await QueryScalarAsync(
            $"SELECT actor FROM events WHERE task_id = '{id}' AND event_type = 'reopened'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsReopenedTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "close", id);
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", id);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal("open", root.GetProperty("status").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("closedAt").ValueKind);
    }

    [Fact]
    public async Task NotClosed_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", id);

        // assert
        result.AssertError($"Task '{id}' is not closed or archived.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "reopen", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
