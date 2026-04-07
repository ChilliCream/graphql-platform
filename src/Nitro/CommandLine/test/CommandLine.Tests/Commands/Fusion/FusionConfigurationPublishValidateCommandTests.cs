using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishValidateCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a Fusion configuration against the schema and clients.

            Usage:
              nitro fusion publish validate [options]

            Options:
              --request-id <request-id>                            The ID of a request [env: NITRO_REQUEST_ID]
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information

            Example:
              nitro fusion publish validate --archive ./gateway.far
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ArchiveFileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "validate", "--archive", ArchiveFile, "--request-id", RequestId);

        // assert
        result.AssertError(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
    }

    [Fact]
    public async Task FusionConfigurationValidationThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "validate", "--archive", ArchiveFile, "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidationErrors))]
    public async Task FusionConfigurationValidationHasErrors_ReturnsError(
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "validate", "--archive", ArchiveFile, "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_ValidationSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
    }

    [Fact]
    public async Task Subscription_ValidationSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Subscription_ValidationFailed_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_1>(MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        var failedEvent = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationValidationFailed>(MockBehavior.Strict);
        failedEvent.SetupGet(x => x.Errors).Returns(
            new IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_1[]
            {
                errorMock.Object
            });

        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(failedEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
                └── Something went wrong.
            """);
        result.StdErr.MatchInlineSnapshot("");
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Queued_ThrowsExitException()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(CreateQueuedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request is in the queued state. Try to run `fusion-configuration publish start` once the request is ready
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_AlreadyFailed_ThrowsExitException()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(CreatePublishingFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request has already failed
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_AlreadyPublished_ThrowsExitException()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(CreatePublishingSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            You request is already published
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Ready_ThrowsExitException()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(CreateReadyEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Your request is ready for the composition. Run `fusion-configuration publish start`
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateOperationInProgressEvent(),
            CreateValidationInProgressEvent(),
            CreateValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ThrowsExitException()
    {
        // arrange
        var unknownEvent = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration
            ├── ! Unknown server response. Consider updating the CLI.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Validate_Should_HandleApprovalEvents_When_WaitForApproval()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateWaitForApprovalEvent(),
            CreateProcessingTaskApprovedEvent(),
            CreateValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "validate",
            "--archive",
            ArchiveFile,
            "--request-id",
            RequestId);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration
            └── ✕ Failed to validate the Fusion configuration.
            """);
    }

    #region Theory Data

    public static TheoryData<
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors,
        string> GetValidationErrors() => new()
    {
        { CreateValidationUnauthorizedError(), "Unauthorized." },
        { CreateValidationRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateValidationInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    #endregion
}
