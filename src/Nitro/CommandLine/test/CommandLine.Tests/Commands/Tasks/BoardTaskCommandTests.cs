namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class BoardTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "board", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Open the interactive task board.

            Usage:
              nitro task board [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro task board
            """);
    }

    [Fact]
    public async Task NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "board");

        // assert
        result.AssertError("task board requires an interactive terminal.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("task", "board");

        // assert
        result.AssertError("No task workspace found. Run `nitro task init` first.");
    }
}
