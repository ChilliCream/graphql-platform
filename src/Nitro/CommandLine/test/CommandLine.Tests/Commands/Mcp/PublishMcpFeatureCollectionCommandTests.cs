using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class PublishMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish an MCP feature collection version to a stage.

            Usage:
              nitro mcp publish [options]

            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                                          The name of the stage [env: NITRO_STAGE]
              --force                                                             Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                                                 Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information

            Example:
              nitro mcp publish \
                --mcp-feature-collection-id "<collection-id>" \
                --stage "dev" \
                --tag "v1"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ForceAndWaitForApproval_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--force",
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The '--force' and '--wait-for-approval' options are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionPublishThrows_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetStartMcpFeatureCollectionPublishErrors))]
    public async Task StartMcpFeatureCollectionPublishHasErrors_ReturnsError(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task StartMcpFeatureCollectionPublishReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            │       └── Something went wrong during publish.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_QueuePosition_UpdatesActivity()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishQueuedEvent(3),
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Queued at position 3.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ReadyState_PrintsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishReadyEvent(),
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Ready.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ApprovedState_UpdatesActivity()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishApprovedEvent(),
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishWaitForApprovalEvent(),
            CreateMcpFeatureCollectionPublishApprovedEvent(),
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── 🕐 Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_ForceEnabled_ShowsWarning()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(force: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_WaitForApprovalThenApproved_ReturnsSuccess()
    {
        // arrange
        SetupPublishMcpFeatureCollectionMutation(waitForApproval: true);
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishWaitForApprovalEvent(),
            CreateMcpFeatureCollectionPublishApprovedEvent(),
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── 🕐 Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithValidationError_ReturnsError()
    {
        // arrange
        var validationError = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        validationError.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(Array.Empty<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections>());

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishFailedEvent(validationError.Object));

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_FailedWithTimeoutError_ReturnsError()
    {
        // arrange
        var timeoutError = new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors_ProcessingTimeoutError(
            "ProcessingTimeoutError",
            "The operation timed out.");

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishFailedEvent(timeoutError));

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            │       └── The operation timed out.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_FailedWithConcurrentOpError_ReturnsError()
    {
        // arrange
        var concurrentError = new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors_ConcurrentOperationError(
            "ConcurrentOperationError",
            "A concurrent operation is in progress.");

        SetupPublishMcpFeatureCollectionMutation();
        SetupPublishMcpFeatureCollectionSubscription(
            CreateMcpFeatureCollectionPublishOperationInProgressEvent(),
            CreateMcpFeatureCollectionPublishFailedEvent(concurrentError));

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            │       └── A concurrent operation is in progress.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP feature collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors, string>
        GetStartMcpFeatureCollectionPublishErrors()
    {
        var unexpectedError = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                "Not authorized to publish."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    Stage),
                "Stage not found."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    McpFeatureCollectionId,
                    "MCP Feature Collection not found."),
                "MCP Feature Collection not found."
            },
            {
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionVersionNotFoundError(
                    Tag,
                    "MCP Feature Collection version not found.",
                    McpFeatureCollectionId),
                "MCP Feature Collection version not found."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
