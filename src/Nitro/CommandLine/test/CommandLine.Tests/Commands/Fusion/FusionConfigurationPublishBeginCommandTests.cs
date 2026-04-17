using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishBeginCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--help");

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

            Example:
              nitro fusion publish begin \
                --api-id "<api-id>" \
                --tag "v1" \
                --stage "dev"
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
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task RequestDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupRequestDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'dev' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetRequestDeploymentSlotErrors))]
    public async Task RequestDeploymentSlotHasErrors_ReturnsError(
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupRequestDeploymentSlotMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "begin",
            "--api-id", ApiId, "--stage", Stage, "--tag", Tag);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'dev' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);
        payload.SetupGet(x => x.RequestId).Returns((string?)null);

        FusionConfigurationClientMock
            .Setup(x => x.RequestDeploymentSlotAsync(
                ApiId,
                Stage,
                Tag,
                null,
                null,
                It.IsAny<SourceSchemaVersion[]>(),
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Requesting deployment slot for stage 'dev' of API 'api-1'
            └── ✕ Failed to request a deployment slot.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Success_DeploymentSlotReady_ReturnsSuccess()
    {
        // arrange
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.AssertSuccess(
            """
            Requesting deployment slot for stage 'dev' of API 'api-1'
            ├── Publication request created. (ID: request-id)
            └── ✓ Deployment slot ready.

            {
              "requestId": "request-id"
            }
            """);
    }

    [Fact]
    public async Task Success_DeploymentSlotReady_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.AssertSuccess(
            """
            {
              "requestId": "request-id"
            }
            """);
    }

    [Fact]
    public async Task Begin_Should_HandleQueuePosition_When_ProcessingTaskIsQueued_ReturnsSuccess()
    {
        // arrange
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription(
            CreateQueuedEvent(3),
            CreateReadyEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.AssertSuccess(
            """
            Requesting deployment slot for stage 'dev' of API 'api-1'
            ├── Publication request created. (ID: request-id)
            ├── ⏳ Your request is queued at position 3.
            └── ✓ Deployment slot ready.

            {
              "requestId": "request-id"
            }
            """);
    }

    [Fact]
    public async Task Begin_Should_PassSubgraphId_When_Provided_ReturnsSuccess()
    {
        // arrange
        SetupRequestDeploymentSlotMutation(subgraphId: "subgraph-1");
        SetupRequestDeploymentSlotSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--subgraph-id",
            "subgraph-1");

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Begin_Should_PassSubgraphName_When_Provided_ReturnsSuccess()
    {
        // arrange
        SetupRequestDeploymentSlotMutation(subgraphName: "subgraph-1");
        SetupRequestDeploymentSlotSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--subgraph-name",
            "subgraph-1");

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Begin_Should_PassWaitForApproval_When_Provided_ReturnsSuccess()
    {
        // arrange
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "begin",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval");

        // assert
        Assert.Equal(0, result.ExitCode);
    }

    #region Theory Data

    public static TheoryData<
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors,
        string> GetRequestDeploymentSlotErrors() => new()
    {
        { CreateRequestDeploymentSlotUnauthorizedError(), "Unauthorized." },
        { CreateRequestDeploymentSlotApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreateRequestDeploymentSlotStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreateRequestDeploymentSlotSubgraphInvalidError(), "Subgraph is invalid." },
        { CreateRequestDeploymentSlotInvalidStateTransitionError(), "Invalid processing state transition." },
        { CreateRequestDeploymentSlotInvalidSourceMetadataError(), "Invalid source metadata input." }
    };

    #endregion
}
