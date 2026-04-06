using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCommitCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "publish", "commit", "--help");

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

            Example:
              nitro fusion publish commit --archive ./gateway.far
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
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

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
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--archive",
            ArchiveFile);

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
    public async Task ArchiveFileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
    }

    [Fact]
    public async Task FusionConfigurationUploadThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "commit", "--archive", ArchiveFile, "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadErrors))]
    public async Task FusionConfigurationUploadHasErrors_ReturnsError(
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "commit",
            "--request-id", RequestId, "--archive", ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Success_CommitsArchive_NonInteractive()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration
            └── ✓ Published Fusion configuration.
            """);
    }

    [Fact]
    public async Task Success_CommitsArchive_Interactive()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Success_CommitsArchive_JsonOutput()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task RequestIdFromStateFile_Success(InteractionMode mode)
    {
        // arrange
        SetupFusionPublishingStateCache(RequestId);
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Commit_Should_ReturnError_When_CommitFails()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        FusionConfigurationClientMock
            .Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .Returns(Array.Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>()
                .ToAsyncEnumerable());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The commit has failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Commit_Should_HandleSubscriptionEvents_When_PublishFails()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreatePublishingFailedEvent(CreatePublishingGenericError("Deployment failed.")));

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
                └── Deployment failed.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Commit_Should_HandleSubscriptionEvents_When_Queued()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateQueuedEvent(2),
            CreatPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration
            ├── Queued at position 2.
            └── ✓ Published Fusion configuration.
            """);
    }

    #region Theory Data

    public static TheoryData<
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors,
        string> GetUploadErrors() => new()
    {
        { CreateUploadUnauthorizedError(), "Unauthorized." },
        { CreateUploadRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateUploadInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    #endregion
}
