
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Agent.Tasks;

public sealed class RemoveTaskLabelCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Remove a label from a task.

            Usage:
              nitro agent tasks label remove <id> <label> [options]

            Arguments:
              <id>     The task ID
              <label>  The label

            Options:
              --actor <actor> (REQUIRED)  The actor recorded on the audit log; allocate one with `nitro agent login`
              --output <json>             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help              Show help and usage information

            Example:
              nitro agent tasks label remove "acme-1a2" api
            """);
    }

    [Fact]
    public async Task ExistingLabel_RemovesSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", id, "api");

        // assert
        result.AssertSuccess($"✓ Removed label 'api' from '{id}'.");
        Assert.Equal("0", await QueryScalarAsync(
            $"SELECT COUNT(*) FROM labels WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task UppercaseLabel_MatchesNormalizedStoredLabel()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", id, "API");

        // assert
        result.AssertSuccess($"✓ Removed label 'api' from '{id}'.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsMinimalLabelChange()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", id, "api");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{id}}",
              "label": "api"
            }
            """);
    }

    [Fact]
    public async Task LabelNotOnTask_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", id, "api");

        // assert
        result.AssertError($"Label 'api' is not on '{id}'.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "remove", "acme-999", "api");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
