using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCommitCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string ArchiveFilePath = "/tmp/gateway.far";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Commit a Fusion configuration publish.

            Usage:
              nitro fusion publish commit [options]

            Options:
              --request-id <request-id>                            The ID of a request [env: NITRO_REQUEST_ID]
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information
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
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoRequestId_And_NoStateFile_ReturnsError(InteractionMode mode)
    {
        // arrange
        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists(cacheFile)).Returns(false);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            No request ID was provided and no request ID was found in the cache. Please
            provide a request ID.
            """);

        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsUnauthorizedError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.As<IUnauthorizedOperation>().SetupGet(x => x.Message).Returns("Not authorized.");
        error.As<IError>().SetupGet(x => x.Message).Returns("Not authorized.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Not authorized.
            Failed to commit Fusion archive.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsRequestNotFoundError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        error.As<IFusionConfigurationRequestNotFoundError>().SetupGet(x => x.Message).Returns("Request not found.");
        error.As<IError>().SetupGet(x => x.Message).Returns("Request not found.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Request not found.
            Failed to commit Fusion archive.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_CommitsArchive_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_CommitsArchive_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_CommitsArchive_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--request-id",
                "req-1",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task RequestIdFromStateFile_Success(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup(fromStateFile: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "fusion",
                "publish",
                "commit",
                "--archive",
                ArchiveFilePath)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateSuccessSetup(
        bool fromStateFile = false)
    {
        var archiveStream = new MemoryStream("archive-content"u8.ToArray());

        var successEvent = Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>();

        var commitPayload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);
        commitPayload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors>?)null);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.CommitFusionArchiveAsync(
                "req-1",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitPayload.Object);
        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                "req-1",
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>(successEvent));

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(ArchiveFilePath))
            .Returns(archiveStream);

        if (fromStateFile)
        {
            var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
            fileSystem.Setup(x => x.FileExists(cacheFile)).Returns(true);
            fileSystem.Setup(x => x.ReadAllTextAsync(cacheFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("req-1");
        }

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateMutationErrorSetup(
        params ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors[] errors)
    {
        var archiveStream = new MemoryStream("archive-content"u8.ToArray());

        var commitPayload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);
        commitPayload.SetupGet(x => x.Errors).Returns(errors);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.CommitFusionArchiveAsync(
                "req-1",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitPayload.Object);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(ArchiveFilePath))
            .Returns(archiveStream);

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateExceptionSetup(
        Exception ex)
    {
        var archiveStream = new MemoryStream("archive-content"u8.ToArray());

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.CommitFusionArchiveAsync(
                "req-1",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(ArchiveFilePath))
            .Returns(archiveStream);

        return (client, fileSystem);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
