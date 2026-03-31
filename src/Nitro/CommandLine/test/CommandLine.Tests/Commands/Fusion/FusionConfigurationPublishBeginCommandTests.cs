using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishBeginCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private static readonly string[] _baseArgs =
    [
        "fusion",
        "publish",
        "begin",
        "--api-id",
        "api-1",
        "--stage",
        "prod",
        "--tag",
        "v1"
    ];

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "fusion",
                "publish",
                "begin",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Begin a configuration publish. This command will request a deployment slot.

            Usage:
              nitro fusion publish begin [options]

            Options:
              --tag <tag> (REQUIRED)        The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --wait-for-approval           Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information
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
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
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
        var client = CreateExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
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
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(_baseArgs)
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
        var client = CreateExceptionClient(new NitroClientAuthorizationException());
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
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
        var client = CreateExceptionClient(new NitroClientAuthorizationException());
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
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
        var client = CreateExceptionClient(new NitroClientAuthorizationException());
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(_baseArgs)
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
        var error = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.As<IUnauthorizedOperation>().SetupGet(x => x.Message).Returns("Not authorized.");
        error.As<IError>().SetupGet(x => x.Message).Returns("Not authorized.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Not authorized.
            Failed to request deployment slot.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsApiNotFoundError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors_ApiNotFoundError>(MockBehavior.Strict);
        error.As<IApiNotFoundError>().SetupGet(x => x.Message).Returns("API not found.");
        error.As<IError>().SetupGet(x => x.Message).Returns("API not found.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            API not found.
            Failed to request deployment slot.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateNullRequestIdSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to request deployment slot.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_DeploymentSlotReady_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'prod' of API 'api-1'
            ├── Request ID: request-123
            ├── Deployment slot ready.
            └── ✕ Failed to request a deployment slot.

            {
              "requestId": "request-123"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_DeploymentSlotReady_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.AssertSuccessful();

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_DeploymentSlotReady_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateSuccessSetup();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateSuccessSetup()
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);
        payload.SetupGet(x => x.RequestId).Returns("request-123");

        var readyEvent = Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>();

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                "api-1",
                "prod",
                "v1",
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);
        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                "request-123",
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>(readyEvent));

        var cacheFile = Path.Combine(Path.GetTempPath(), "fusion.configuration.publishing.state");
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.WriteAllTextAsync(
                cacheFile,
                "request-123",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateMutationErrorSetup(
        params IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors[] errors)
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.RequestId).Returns((string?)null);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                "api-1",
                "prod",
                "v1",
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem) CreateNullRequestIdSetup()
    {
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);
        payload.SetupGet(x => x.RequestId).Returns((string?)null);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                "api-1",
                "prod",
                "v1",
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        return (client, fileSystem);
    }

    private static Mock<IFusionConfigurationClient> CreateExceptionClient(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.RequestDeploymentSlotAsync(
                "api-1",
                "prod",
                "v1",
                null,
                null,
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
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
