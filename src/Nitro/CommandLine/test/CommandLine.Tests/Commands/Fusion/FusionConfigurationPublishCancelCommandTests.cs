using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCancelCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "publish", "cancel", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Cancel a Fusion configuration publish.

            Usage:
              nitro fusion publish cancel [options]

            Options:
              --request-id <request-id>  The ID of a request [env: NITRO_REQUEST_ID]
              --cloud-url <cloud-url>    The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>        The API key used for authentication [env: NITRO_API_KEY]
              --output <json>            The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help             Show help and usage information

            Example:
              nitro fusion publish cancel
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "cancel",
            "--request-id",
            RequestId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoRequestId_And_NoStateFile_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupFusionPublishingStateCacheMiss();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync("fusion", "publish", "cancel");

        // assert
        result.AssertError(
            """
            No request ID was provided and no request ID was found in the cache. Please provide a request ID.
            """);
    }

    [Fact]
    public async Task ReleaseDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupReleaseDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "cancel", "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Canceling publication
            └── ✕ Failed to cancel the publication.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetReleaseDeploymentSlotErrors))]
    public async Task ReleaseDeploymentSlotHasErrors_ReturnsError(
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupReleaseDeploymentSlotMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "cancel", "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Canceling publication
            └── ✕ Failed to cancel the publication.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task RequestIdFromArg_Success()
    {
        // arrange
        SetupFusionPublishingStateCache(RequestId);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync("fusion", "publish", "cancel", "--request-id", RequestId);

        // assert
        result.AssertSuccess(
            """
            Canceling publication
            └── ✓ Canceled publication for request 'request-id'.
            """);
    }

    [Fact]
    public async Task RequestIdFromStateFile_Success()
    {
        // arrange
        SetupFusionPublishingStateCache(RequestId);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync("fusion", "publish", "cancel");

        // assert
        result.AssertSuccess(
            """
            Canceling publication
            └── ✓ Canceled publication for request 'request-id'.
            """);
    }

    #region Theory Data

    public static TheoryData<
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors,
        string> GetReleaseDeploymentSlotErrors() => new()
    {
        { CreateReleaseDeploymentSlotUnauthorizedError(), "Unauthorized." },
        { CreateReleaseDeploymentSlotRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateReleaseDeploymentSlotInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    #endregion
}
