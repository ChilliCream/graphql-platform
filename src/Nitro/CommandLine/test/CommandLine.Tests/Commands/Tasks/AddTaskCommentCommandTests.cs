
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class AddTaskCommentCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "comment", "add", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Add a comment to a task.

            Usage:
              nitro task comment add <id> <text> [options]

            Arguments:
              <id>    The task ID
              <text>  The comment text

            Options:
              --actor <actor>  The acting identity recorded on the audit log (defaults to NITRO_TASK_ACTOR or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task comment add "acme-1a2" "Looks good to me."
            """);
    }

    [Fact]
    public async Task AddsComment_Successfully()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync(
            "task", "comment", "add", id, "Looks good to me.");

        // assert
        result.AssertSuccess($"✓ Added comment to '{id}'.");
        Assert.Equal("Looks good to me.", await QueryScalarAsync(
            $"SELECT text FROM comments WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task AddsComment_RecordsActorAsAuthor()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync(
            "task", "comment", "add", id, "Reviewed.", "--actor", "alice");

        // assert
        result.AssertSuccess($"✓ Added comment to '{id}'.");
        Assert.Equal("alice", await QueryScalarAsync(
            $"SELECT author FROM comments WHERE task_id = '{id}'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsCreatedComment()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "task", "comment", "add", id, "Looks good to me.", "--actor", "alice");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "id": 1,
              "taskId": "{{id}}",
              "author": "alice",
              "text": "Looks good to me.",
              "createdAt": "2026-01-01T00:00:00+00:00"
            }
            """);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyOrWhitespaceText_ReturnsError(string text)
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "comment", "add", id, text);

        // assert
        result.AssertError("The comment text must not be empty.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync(
            "task", "comment", "add", "acme-999", "Looks good to me.");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }
}
