using System.Text;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionDownloadCommandTests
{
    [Fact]
    public async Task Download_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("fusion", "download");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--stage' is required.
            Option '--api-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Download_WithFarOutput_WritesArchiveFromV2Endpoint()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputFile = CreateFilePath(".far");
        const string archiveContent = "fusion-archive-content";

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(archiveContent)));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output-file",
            outputFile);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(archiveContent, await fileSystem.ReadAllTextAsync(outputFile, CancellationToken.None));
        host.Output.Trim().MatchInlineSnapshot(
            $"""
            Downloaded Fusion configuration to: {outputFile}
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Download_WithFgpOutput_WritesArchiveFromLegacyEndpoint()
    {
        // arrange
        var fileSystem = CreateOutputFileSystem();
        var outputFile = CreateFilePath(".fgp");
        const string archiveContent = "legacy-fusion-archive";

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestLegacyFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(archiveContent)));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output-file",
            outputFile);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(archiveContent, await fileSystem.ReadAllTextAsync(outputFile, CancellationToken.None));
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Download_WhenArchiveIsNull_ReturnsError()
    {
        // arrange
        var outputFile = CreateFilePath(".far");
        var fileSystem = new TestFileSystem();

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "fusion",
            "download",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--output-file",
            outputFile);

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The API with the given ID does not exist or does not have a download URL.
            """);
        client.VerifyAll();
        Assert.False(fileSystem.Files.ContainsKey(Path.GetFullPath(outputFile)));
    }

    private static CommandBuilder CreateHost(
        Mock<IFusionConfigurationClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IFusionConfigurationClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static TestFileSystem CreateOutputFileSystem() => new();
}
