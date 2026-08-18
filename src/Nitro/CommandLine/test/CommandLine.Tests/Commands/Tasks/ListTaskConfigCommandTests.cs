
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class ListTaskConfigCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "config", "list", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all configuration values.

            Usage:
              nitro task config list [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task config list
            """);
    }

    [Fact]
    public async Task AfterInit_ListsPrefixConfig()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "config", "list");

        // assert
        result.AssertSuccess("prefix = acme");
    }

    [Fact]
    public async Task WithAdditionalConfig_ListsAllSortedByKey()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("task", "config", "set", "zzz", "last");
        await ExecuteCommandAsync("task", "config", "set", "aaa", "first");

        // act
        var result = await ExecuteCommandAsync("task", "config", "list");

        // assert
        result.AssertSuccess(
            """
            aaa = first
            prefix = acme
            zzz = last
            """);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "config", "list");

        // assert
        result.AssertError(
            """
            No task workspace found. Run `nitro task init` first.
            """);
    }

    [Fact]
    public async Task JsonOutput_ReturnsStructuredList()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "config", "list");

        // assert
        result.AssertSuccess(
            """
            {
              "items": [
                {
                  "key": "prefix",
                  "value": "acme"
                }
              ]
            }
            """);
    }
}
