
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskLabelCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "label", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List a task's labels, or every label in use.

            Usage:
              nitro task label list [<id>] [options]

            Arguments:
              <id>  The task ID

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task label list
              nitro task label list "acme-1a2"
            """);
    }

    [Fact]
    public async Task WithId_ListsSortedLabels()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "label", "add", id, "parser", "api");

        // act
        var result = await ExecuteCommandAsync("task", "label", "list", id);

        // assert
        result.AssertSuccess(
            """
            api
            parser
            """);
    }

    [Fact]
    public async Task WithIdAndNoLabels_PrintsNoLabels()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "label", "list", id);

        // assert
        result.AssertSuccess("No labels.");
    }

    [Fact]
    public async Task WithoutId_ListsAllLabelsWithCounts()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("task", "label", "add", id1, "api", "parser");
        await ExecuteCommandAsync("task", "label", "add", id2, "api");

        // act
        var result = await ExecuteCommandAsync("task", "label", "list");

        // assert
        result.AssertSuccess(
            """
            api  2
            parser  1
            """);
    }

    [Fact]
    public async Task WithoutIdAndNoLabels_PrintsNoLabels()
    {
        // arrange
        await InitWorkspaceAsync();
        await CreateTaskAsync("Fix the parser");

        // act
        var result = await ExecuteCommandAsync("task", "label", "list");

        // assert
        result.AssertSuccess("No labels.");
    }

    [Fact]
    public async Task TaskNotFound_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "label", "list", "acme-999");

        // assert
        result.AssertError("Task 'acme-999' does not exist.");
    }

    [Fact]
    public async Task JsonOutput_WithId_ReturnsStructuredLabels()
    {
        // arrange
        await InitWorkspaceAsync();
        var id = await CreateTaskAsync("Fix the parser");
        await ExecuteCommandAsync("task", "label", "add", id, "parser", "api");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "label", "list", id);

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                "api",
                "parser"
              ]
            }
            """);
    }

    [Fact]
    public async Task JsonOutput_WithoutId_ReturnsStructuredCounts()
    {
        // arrange
        await InitWorkspaceAsync();
        var id1 = await CreateTaskAsync("Fix the parser");
        var id2 = await CreateTaskAsync("Write the docs");
        await ExecuteCommandAsync("task", "label", "add", id1, "api", "parser");
        await ExecuteCommandAsync("task", "label", "add", id2, "api");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "label", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "label": "api",
                  "count": 2
                },
                {
                  "label": "parser",
                  "count": 1
                }
              ]
            }
            """);
    }
}
