
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class AddTaskLabelCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add one or more labels to a task.

            Usage:
              nitro agent tasks label add <id> <labels>... [options]

            Arguments:
              <id>      The task ID
              <labels>  One or more labels

            Options:
              --actor <actor>  The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks label add "acme-1a2" api parser
            """);
    }

    [Fact]
    public async Task SingleLabel_AddsSuccessfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");

        // assert
        result.AssertSuccess($"✓ Added label 'api' to '{id}'.");
        Assert.Equal("api", await QueryScalarAsync(
            $"SELECT label FROM labels WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task MultipleLabels_AddsAllInOneCall()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api", "parser");

        // assert
        result.AssertSuccess(
            $"""
            ✓ Added label 'api' to '{id}'.
            ✓ Added label 'parser' to '{id}'.
            """);
        Assert.Equal("2", await QueryScalarAsync(
            $"SELECT COUNT(*) FROM labels WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task UppercaseLabelWithWhitespace_IsNormalizedBeforeStorage()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "  API  ");

        // assert
        result.AssertSuccess($"✓ Added label 'api' to '{id}'.");
        Assert.Equal("api", await QueryScalarAsync(
            $"SELECT label FROM labels WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task ExistingLabel_PrintsAlreadyOnMessage()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api");

        // assert
        result.AssertSuccess($"Label 'api' is already on '{id}'.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsMinimalLabelChanges()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "api", "parser");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": "{{id}}",
              "labels": [
                {
                  "label": "api",
                  "added": true
                },
                {
                  "label": "parser",
                  "added": true
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task EmptyLabel_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", id, "   ");

        // assert
        result.AssertError("Labels must be non-empty.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "label", "add", "acme-999", "api");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
