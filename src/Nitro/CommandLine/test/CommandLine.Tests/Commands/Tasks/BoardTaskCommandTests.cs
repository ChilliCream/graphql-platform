namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class BoardTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "tasks", "board", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Open the interactive task board.

            Usage:
              nitro agent tasks board [options]

            Options:
              -?, -h, --help  Show help and usage information

            Example:
              nitro agent tasks board
            """);
    }

    [Fact]
    public async Task NonInteractive_ReturnsError()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "board");

        // assert
        result.AssertError("agent tasks board requires an interactive terminal.");
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync("agent", "tasks", "board");

        // assert
        result.AssertError("No task workspace found. Run `nitro agent tasks init` first.");
    }
}
