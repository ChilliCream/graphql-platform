using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionRunCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "run",
                "--help")
            .ExecuteAsync();

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
    public async Task Run_Should_ReturnError_When_ArchiveFileNotFound()
    {
        // arrange
        const string archiveFile = "nonexistent.far";

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists(archiveFile))
            .Returns(false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(fileSystem.Object)
            .AddArguments(
                "fusion",
                "run",
                archiveFile)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Archive file 'nonexistent.far' does not exist.
            """);

        fileSystem.VerifyAll();
    }
}
