namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishStartCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "publish", "start", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Start a Fusion configuration publish.

            Usage:
              nitro fusion publish start [options]

            Options:
              --request-id <request-id>  The ID of a request [env: NITRO_REQUEST_ID]
              --cloud-url <cloud-url>    The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>        The API key used for authentication [env: NITRO_API_KEY]
              --output <json>            The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help             Show help and usage information

            Example:
              nitro fusion publish start
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
            "start",
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
        var result = await ExecuteCommandAsync("fusion", "publish", "start");

        // assert
        result.AssertError(
            """
            No request ID was provided and no request ID was found in the cache. Please provide a request ID.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task RequestIdFromStateFile_Success(InteractionMode mode)
    {
        // arrange
        SetupFusionPublishingStateCache(RequestId);
        SetupClaimDeploymentSlotMutation();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync("fusion", "publish", "start");

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task ClaimDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupClaimDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "start", "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Starting composition
            └── ✕ Failed to start the composition.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Success_WithRequestIdOption()
    {
        // arrange
        SetupClaimDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "start",
            "--request-id",
            RequestId);

        // assert
        result.AssertSuccess(
            """
            Starting composition
            └── ✓ Started composition for request 'request-id'.
            """);
    }
}
