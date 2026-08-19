
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class UndeferTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "undefer", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Make a deferred task ready again.

            Usage:
              nitro task undefer <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --actor <actor>  The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task undefer "acme-1a2"
            """);
    }

    [Fact]
    public async Task DeferredTask_UndefersSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "defer", id, "--until", "2026-02-01");

        // act
        var result = await ExecuteCommandAsync("task", "undefer", id);

        // assert
        result.AssertSuccess($"✓ Undeferred task '{id}'.");
        Assert.Equal("open", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
        Assert.Null(await QueryScalarAsync(
            $"SELECT defer_until FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsUndeferredTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "defer", id, "--until", "2026-02-01");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "undefer", id);

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal("open", root.GetProperty("status").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("deferUntil").ValueKind);
    }

    [Fact]
    public async Task OpenTask_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "undefer", id);

        // assert
        result.AssertError($"Task '{id}' is not deferred.");
    }

    [Fact]
    public async Task ClosedTask_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "close", id);

        // act
        var result = await ExecuteCommandAsync("task", "undefer", id);

        // assert
        result.AssertError($"Task '{id}' is not deferred.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "undefer", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
