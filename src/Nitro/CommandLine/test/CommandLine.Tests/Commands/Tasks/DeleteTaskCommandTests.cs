
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class DeleteTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "delete", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Delete a task.

            Usage:
              nitro task delete <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --reason <reason>  The reason recorded for this change
              --actor <actor>    The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --force            Skip confirmation prompts for deletes and overwrites
              --output <json>    The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help     Show help and usage information

            Example:
              nitro task delete "app-1a2"
              nitro task delete "app-1a2" --force
            """);
    }

    [Fact]
    public async Task WithForce_DeletesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "delete", id, "--force");

        // assert
        result.AssertSuccess($"✓ Deleted task '{id}'.");
        Assert.Equal("tombstone", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task WithReason_RecordsDeleteReason()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync(
            "task", "delete", id, "--force", "--reason", "Duplicate of acme-1");

        // assert
        result.AssertSuccess($"✓ Deleted task '{id}'.");
        Assert.Equal("Duplicate of acme-1", await QueryScalarAsync(
            $"SELECT delete_reason FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsDeletedTaskSnapshot()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "delete", id, "--force");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(id, root.GetProperty("id").GetString());
        Assert.Equal("tombstone", root.GetProperty("status").GetString());
    }

    [Fact]
    public async Task WithoutForce_NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "delete", id);

        // assert
        result.AssertError("Use --force to delete without confirmation.");
    }

    [Fact]
    public async Task WithoutForce_Interactive_Confirmed_DeletesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("task", "delete", id);

        // act
        command.Confirm(true);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        result.AssertSuccess(
            $"""
            ? Delete task '{id}'? [y/n] (y): y
            ✓ Deleted task '{id}'.
            """);
    }

    [Fact]
    public async Task WithoutForce_Interactive_Declined_Aborts()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand("task", "delete", id);

        // act
        command.Confirm(false);
        var result = await command.RunToCompletionAsync(TestContext.Current.CancellationToken);

        // assert
        result.AssertSuccess(
            $"""
            ? Delete task '{id}'? [y/n] (y): n
            Aborted.
            """);
        Assert.Equal("open", await QueryScalarAsync(
            $"SELECT status FROM tasks WHERE id = '{id}'"));
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "delete", "acme-999", "--force");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
