using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

// TODO: Needs test with --version parameter
public sealed class FusionDownloadCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "download",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download the most recent gateway configuration.

            Usage:
              nitro fusion download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --version <version>           The version of the archive to download [default: 2.0.0]
              --output-file <output-file>   The file path to write the output to [env: NITRO_OUTPUT_FILE]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro fusion download \
                --api-id "<api-id>" \
                --stage "dev" \
                --output-file ./gateway.far
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task WithOptions_DownloadsFarFile_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_DownloadsFarFile_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Downloading latest Fusion configuration from stage 'prod' of API 'api-1'
            └── ✓ Downloaded Fusion configuration from stage 'prod'.
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_DownloadsFarFile_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "file": "/tmp/gateway.far"
            }
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithFgpExtension_DownloadsLegacyArchive_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.fgp")
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithFgpExtension_DownloadsLegacyArchive_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.fgp")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Downloading latest Fusion configuration from stage 'prod' of API 'api-1'
            └── ✓ Downloaded Fusion configuration from stage 'prod'.
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithFgpExtension_DownloadsLegacyArchive_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = SetupDownloadMocks(legacy: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.fgp")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "file": "/tmp/gateway.fgp"
            }
            """);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    public async Task ExistingFileDeleted_BeforeWrite(InteractionMode mode)
    {
        // arrange
        var downloadStream = new MemoryStream("archive-content"u8.ToArray());
        var fileStream = new MemoryStream();

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadStream);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("/tmp/gateway.far"))
            .Returns(true);
        fileSystem.Setup(x => x.DeleteFile("/tmp/gateway.far"));
        fileSystem.Setup(x => x.CreateFile("/tmp/gateway.far"))
            .Returns(fileStream);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.StdErr);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task StreamIsNull_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The API with the given ID does not exist or does not have a download URL.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Downloading latest Fusion configuration from stage 'prod' of API 'api-1'
            └── ✕ Failed to download the latest Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "prod",
                "--output-file",
                "/tmp/gateway.far")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Downloading latest Fusion configuration from stage 'prod' of API 'api-1'
            └── ✕ Failed to download the latest Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) SetupDownloadMocks(
        bool legacy)
    {
        var downloadStream = new MemoryStream("archive-content"u8.ToArray());
        var fileStream = new MemoryStream();

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);

        if (legacy)
        {
            client.Setup(x => x.DownloadLatestLegacyFusionArchiveAsync(
                    "api-1",
                    "prod",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadStream);
        }
        else
        {
            client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                    "api-1",
                    "prod",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(downloadStream);
        }

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists(It.IsAny<string>()))
            .Returns(false);
        fileSystem.Setup(x => x.CreateFile(It.IsAny<string>()))
            .Returns(fileStream);

        return (client, fileSystem);
    }

    private static Mock<IFusionConfigurationClient> CreateExceptionClient(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestFusionArchiveAsync(
                "api-1",
                "prod",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
