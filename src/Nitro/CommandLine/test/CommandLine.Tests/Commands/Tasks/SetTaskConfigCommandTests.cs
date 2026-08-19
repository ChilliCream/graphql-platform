
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class SetTaskConfigCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "config", "set", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Set a configuration value.

            Usage:
              nitro task config set <key> <value> [options]

            Arguments:
              <key>    The configuration key
              <value>  The configuration value

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task config set prefix "app"
            """);
    }

    [Fact]
    public async Task NewKey_SetsAndConfirms()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "config", "set", "reviewer", "alice");

        // assert
        result.AssertSuccess("✓ Set 'reviewer' to 'alice'.");
        Assert.Equal("alice", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'reviewer'"));
    }

    [Fact]
    public async Task ExistingKey_OverwritesValue()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("task", "config", "set", "reviewer", "alice");

        // act
        var result = await ExecuteCommandAsync("task", "config", "set", "reviewer", "bob");

        // assert
        result.AssertSuccess("✓ Set 'reviewer' to 'bob'.");
        Assert.Equal("bob", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'reviewer'"));
    }

    [Fact]
    public async Task JsonOutput_ReturnsSetKeyAndValue()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "config", "set", "reviewer", "alice");

        // assert
        result.AssertSuccess(
            """
            {
              "key": "reviewer",
              "value": "alice"
            }
            """);
    }

    [Fact]
    public async Task PrefixKey_NormalizesValue()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "config", "set", "prefix", "My App!");

        // assert
        result.AssertSuccess("✓ Set 'prefix' to 'myapp'.");
        Assert.Equal("myapp", await QueryScalarAsync(
            "SELECT value FROM config WHERE key = 'prefix'"));
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "config", "set", "reviewer", "alice");

        // assert
        result.AssertError(
            """
            No task workspace found. Run `nitro task init` first.
            """);
    }
}
