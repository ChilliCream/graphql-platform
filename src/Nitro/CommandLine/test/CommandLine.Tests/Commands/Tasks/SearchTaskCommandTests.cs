
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class SearchTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "search", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Search tasks by text.

            Usage:
              nitro task search <text> [options]

            Arguments:
              <text>  The search text

            Options:
              --limit <limit>  The maximum number of tasks to show
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task search "parser"
              nitro task search "parser" --limit 5
            """);
    }

    [Fact]
    public async Task NoMatches_PrintsNoTasksFound()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "search", "nonexistent");

        // assert
        result.AssertSuccess("No tasks found.");
    }

    [Fact]
    public async Task MatchesTitle_CaseInsensitive()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the Parser");
        await CreateTaskAsync("Write the docs");

        // act
        var result = await ExecuteCommandAsync("task", "search", "parser");

        // assert
        result.AssertSuccess(
            $"""
            {id}  P2  task  open  Fix the Parser

            1 task(s)
            """);
    }

    [Fact]
    public async Task MatchesDescription_ReturnsTask()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync(
            "Improve error messages", "--description", "Handle edge cases in the tokenizer");

        // act
        var result = await ExecuteCommandAsync("task", "search", "tokenizer");

        // assert
        result.AssertSuccess(
            $"""
            {id}  P2  task  open  Improve error messages

            1 task(s)
            """);
    }

    [Fact]
    public async Task TombstonedTask_ExcludedFromResults()
    {
        // arrange
        await InitWorkspaceAsync();
        var deletedId = await CreateTaskAsync("Widget cleanup");
        await ExecuteCommandAsync("task", "delete", deletedId, "--force");

        // act
        var result = await ExecuteCommandAsync("task", "search", "widget");

        // assert
        result.AssertSuccess("No tasks found.");
    }

    [Fact]
    public async Task LimitOption_TruncatesResultsSortedByPriority()
    {
        // arrange
        await InitWorkspaceAsync();
        var high = await CreateTaskAsync("Widget alpha", "--priority", "p0");
        var mid = await CreateTaskAsync("Widget beta", "--priority", "p1");
        await CreateTaskAsync("Widget gamma", "--priority", "p2");

        // act
        var result = await ExecuteCommandAsync("task", "search", "widget", "--limit", "2");

        // assert
        result.AssertSuccess(
            $"""
            {high}  P0  task  open  Widget alpha
            {mid}  P1  task  open  Widget beta

            2 task(s)
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the Parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "search", "parser");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "items": [
                {
                  "id": "{{id}}",
                  "priority": 2,
                  "type": "task",
                  "status": "open",
                  "title": "Fix the Parser"
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_NoMatches_ReturnsEmptyItems()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "search", "nonexistent");

        // assert
        result.AssertSuccess(
            """
            {
              "items": []
            }
            """);
    }

    [Fact]
    public async Task LiteralPercent_DoesNotActAsWildcard()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("100% done");
        await CreateTaskAsync("Something else");

        // act
        var result = await ExecuteCommandAsync("task", "search", "100%");

        // assert
        result.AssertSuccess(
            $"""
            {id}  P2  task  open  100% done

            1 task(s)
            """);
    }
}
