
namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Tasks;

public sealed class WhereTaskCommandTests(NitroCommandFixture fixture)
    : TasksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "where", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Print the absolute path of the current task workspace.

            Usage:
              nitro task where [options]

            Options:
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro task where
            """);
    }

    [Fact]
    public async Task WorkspaceExists_PrintsWorkspaceDirectory()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("task", "where");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);
        Assert.Equal(WorkspaceDirectory, result.StdOut);
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("task", "where");

        // assert
        result.AssertError("No task workspace found. Run `nitro task init` first.");
    }

    [Fact]
    public async Task JsonOutput_WorkspaceExists_ReturnsStructuredPath()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("task", "where");

        // assert
        result.AssertSuccess(
            $$"""
            {
              "path": "{{WorkspaceDirectory.Replace("\\", "\\\\")}}"
            }
            """);
    }
}
