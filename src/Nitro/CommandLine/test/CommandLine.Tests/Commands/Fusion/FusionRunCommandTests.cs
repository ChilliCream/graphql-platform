namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionRunCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "run",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Start a Fusion gateway with the specified archive.
              This command only supports Fusion v2.

            Usage:
              nitro fusion run <ARCHIVE_FILE> [options]

            Arguments:
              <ARCHIVE_FILE>  The path to the Fusion archive file

            Options:
              -p, --port <port>  The port the gateway will listen on
              -?, -h, --help     Show help and usage information

            Example:
              nitro fusion run ./gateway.far --port 5000
            """);
    }

    [Fact]
    public async Task FileDoesNotExist_ReturnsError()
    {
        // arrange
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "run",
            "nonexistent.far");

        // assert
        result.AssertError(
            """
            Archive file 'nonexistent.far' does not exist.
            """);
    }
}
