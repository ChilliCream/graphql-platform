namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

public sealed class BoardMailCommandTests(NitroCommandFixture fixture) : MailCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "mail", "board", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Open the interactive mail board.

            Usage:
              nitro agent mail board [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro agent mail board
            """);
    }

    [Fact]
    public async Task NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "board");

        // assert
        result.AssertError("agent mail board requires an interactive terminal.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("agent", "mail", "board");

        // assert
        result.AssertError("No mail workspace found. Run `nitro agent mail init` first.");
    }
}
