
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskCommentCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List a task's comments.

            Usage:
              nitro agent tasks comment list <id> [options]

            Arguments:
              <id>  The task ID

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks comment list "acme-1a2"
            """);
    }

    [Fact]
    public async Task NoComments_PrintsNoComments()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess("No comments.");
    }

    [Fact]
    public async Task SingleComment_DisplaysFormattedEntry()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "comment", "add", id, "Looks good to me.");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess(
            """
              [1] test-agent 2026-01-01 00:00
                Looks good to me.
            """);
    }

    [Fact]
    public async Task MultipleComments_DisplaysInOrderWithBlankLineBetween()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "comment", "add", id, "Looks good to me.");
        FakeTime.Advance(TimeSpan.FromMinutes(1));
        await ExecuteCommandAsync(
            "agent", "tasks", "comment", "add", id, "Thanks, merging.", "--actor", "alice");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess(
            """
              [1] test-agent 2026-01-01 00:00
                Looks good to me.

              [2] alice 2026-01-01 00:01
                Thanks, merging.
            """);
    }

    [Fact]
    public async Task MultiLineCommentText_IndentsEveryLine()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "comment", "add", id, "Line one\nLine two");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess(
            """
              [1] test-agent 2026-01-01 00:00
                Line one
                Line two
            """);
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredComments()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("agent", "tasks", "comment", "add", id, "Looks good to me.");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "id": 1,
                  "taskId": "{{id}}",
                  "author": "test-agent",
                  "text": "Looks good to me.",
                  "createdAt": "2026-01-01T00:00:00+00:00"
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_NoComments_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "comment", "list", id);

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }
}
