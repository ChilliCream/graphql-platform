
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class GetTaskConfigCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Get a configuration value.

            Usage:
              nitro agent tasks config get <key> [options]

            Arguments:
              <key>  The configuration key

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent tasks config get prefix
            """);
    }

    [Fact]
    public async Task ExistingKey_ReturnsValue()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "prefix");

        // assert
        result.AssertSuccess("acme");
    }

    [Fact]
    public async Task AfterSet_ReturnsSetValue()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "tasks", "config", "set", "reviewer", "alice");

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "reviewer");

        // assert
        result.AssertSuccess("alice");
    }

    [Fact]
    public async Task MissingKey_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "nonexistent");

        // assert
        result.AssertError("Configuration key 'nonexistent' is not set.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "prefix");

        // assert
        result.AssertError(
            """
            No task workspace found. Run `nitro agent tasks init` first.
            """);
    }

    [Fact]
    public async Task JsonOutput_ExistingKey_ReturnsStructuredValue()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "config", "get", "prefix");

        // assert
        result.AssertSuccess(
            """
            {
              "value": "acme"
            }
            """);
    }
}
