using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishValidateCommandTests
{
    private const string DefaultArchiveFile = "fusion.far";
    private const string DefaultRequestId = "req-1";

    private static readonly string[] _baseArgs =
    [
        "fusion",
        "publish",
        "validate",
        "--archive",
        DefaultArchiveFile,
        "--request-id",
        DefaultRequestId
    ];

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "fusion",
                "publish",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validates a Fusion configuration against the schema and clients.

            Usage:
              nitro fusion publish validate [options]

            Options:
              --request-id <request-id>                            The ID of a request [env: NITRO_REQUEST_ID]
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file. (the --configuration alias will be removed in an upcoming version) [env: NITRO_FUSION_CONFIG_FILE]
              --cloud-url <cloud-url>                              The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                      The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information
            """);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientException("validation request failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: validation request failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientException("validation request failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: validation request failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientException("validation request failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: validation request failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Validating...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateExceptionSetup(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsUnauthorizedError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_UnauthorizedOperation>(MockBehavior.Strict);
        error.As<IUnauthorizedOperation>().SetupGet(x => x.Message).Returns("Not authorized.");
        error.As<IError>().SetupGet(x => x.Message).Returns("Not authorized.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Not authorized.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsRequestNotFoundError_ReturnsError_NonInteractive()
    {
        // arrange
        var error = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors_FusionConfigurationRequestNotFoundError>(MockBehavior.Strict);
        error.As<IFusionConfigurationRequestNotFoundError>().SetupGet(x => x.Message).Returns("Request not found.");
        error.As<IError>().SetupGet(x => x.Message).Returns("Request not found.");

        var (client, fileSystem) = CreateMutationErrorSetup(error.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Request not found.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ValidationSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✓ The validation was successful
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ValidationSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] The validation was successful
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ValidationFailed_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_1>(MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong.
            The validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Queued_ThrowsExitException()
    {
        // arrange
        var queuedEvent = new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsQueued(
            ProcessingState.Queued,
            "ProcessingTaskIsQueued",
            "Queued",
            1);

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                queuedEvent
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_AlreadyFailed_ThrowsExitException()
    {
        // arrange
        var failedEvent = new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingFailed(
            ProcessingState.Failed,
            "FusionConfigurationPublishingFailed",
            "Failed",
            Array.Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors>());

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                failedEvent
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request has already failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_AlreadyPublished_ThrowsExitException()
    {
        // arrange
        var successEvent = new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess(
            ProcessingState.Success,
            "FusionConfigurationPublishingSuccess",
            "Success");

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                successEvent
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            You request is already published
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Ready_ThrowsExitException()
    {
        // arrange
        var readyEvent = new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady(
            ProcessingState.Ready,
            "ProcessingTaskIsReady",
            "Ready");

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                readyEvent
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request is ready for the composition. Run `fusion-configuration publish start`
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_OperationInProgress(
                    ProcessingState.Processing,
                    "OperationInProgress"),
                new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ValidationInProgress(
                    ProcessingState.Processing,
                    "ValidationInProgress"),
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            ├── The validation is in progress
            ├── The validation is in progress
            └── ✓ The validation was successful
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ThrowsExitException()
    {
        // arrange
        var unknownEvent = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateSubscriptionSetup(
            CreateSuccessMutationPayload(),
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(_baseArgs)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Unknown response
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static Mock<IFileSystem> CreateFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(DefaultArchiveFile))
            .Returns(new MemoryStream("archive-content"u8.ToArray()));
        return fileSystem;
    }

    private static IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition
        CreateSuccessMutationPayload()
    {
        var payload = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors>?)null);
        return payload.Object;
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateMutationErrorSetup(
            params IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors[] errors)
    {
        var payload = new Mock<IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);

        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateFusionConfigurationPublishAsync(
                DefaultRequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        var fileSystem = CreateFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateSubscriptionSetup(
            IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition mutationPayload,
            IEnumerable<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged> subscriptionEvents)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateFusionConfigurationPublishAsync(
                DefaultRequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
        CreateExceptionSetup(Exception ex)
    {
        var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
        client.Setup(x => x.ValidateFusionConfigurationPublishAsync(
                DefaultRequestId,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateFileSystem();

        return (client, fileSystem);
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateValidationSuccess()
    {
        return new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationSuccess(
            ProcessingState.Success,
            "FusionConfigurationValidationSuccess",
            "Success",
            Array.Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Changes>());
    }

    private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
        CreateValidationFailed(
            params IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_1[] errors)
    {
        return new OnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationFailed(
            ProcessingState.Failed,
            "FusionConfigurationValidationFailed",
            "Failed",
            errors);
    }
}
