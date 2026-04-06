using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class PublishClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a client version to a stage.

            Usage:
              nitro client publish [options]

            Options:
              --client-id <client-id> (REQUIRED)  The ID of the client [env: NITRO_CLIENT_ID]
              --tag <tag> (REQUIRED)              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)          The name of the stage [env: NITRO_STAGE]
              --force                             Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                 Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information

            Example:
              nitro client publish \
                --client-id "<client-id>" \
                --tag "v1" \
                --stage "dev"
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
            "client",
            "publish",
            "--client-id",
            ClientId,
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
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task PublishThrows_ReturnsError()
    {
        // arrange
        SetupPublishClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetMutationErrors))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IPublishClientVersion_PublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupPublishClientMutation(errors: mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupPublishClientMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new client version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_QueuePosition_UpdatesActivity()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishQueuedEvent(3),
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is queued. The current position in the queue is 3.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ReadyState_PrintsSuccess()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishReadyEvent(),
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is ready for processing.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ApprovedState_UpdatesActivity()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishApprovedEvent(),
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity()
    {
        // arrange
        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishWaitForApprovalEvent(),
            CreateClientVersionPublishApprovedEvent(),
            CreateClientVersionPublishOperationInProgressEvent(),
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is waiting for approval. Check Nitro to approve the request.
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupPublishClientMutation();
        SetupPublishClientSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ForceOption_LogsForceEnabled()
    {
        // arrange
        SetupPublishClientMutation(force: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Publish_Should_ReturnError_When_SubscriptionHasConcurrentOperationError()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IConcurrentOperationError>()
            .SetupGet(x => x.Message)
            .Returns("A concurrent operation is already in progress.");

        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            A concurrent operation is already in progress.
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Publish_Should_ReturnError_When_SubscriptionHasValidationError()
    {
        // arrange
        var queryErrorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>(
            MockBehavior.Strict);
        queryErrorMock.SetupGet(x => x.Message).Returns("Field 'foo' does not exist.");
        queryErrorMock.SetupGet(x => x.Code).Returns("FIELD_NOT_FOUND");
        queryErrorMock.SetupGet(x => x.Path).Returns((string?)null);
        queryErrorMock.SetupGet(x => x.Locations)
            .Returns((IReadOnlyList<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>?)null);

        var queryMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        queryMock.SetupGet(x => x.Message).Returns("Query abc123 is invalid.");
        queryMock.SetupGet(x => x.Hash).Returns("abc123");
        queryMock.SetupGet(x => x.DeployedTags).Returns(new List<string>());
        queryMock.SetupGet(x => x.Errors).Returns(new[] { queryErrorMock.Object });

        var clientInfoMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(
            MockBehavior.Strict);
        clientInfoMock.SetupGet(x => x.Id).Returns(ClientId);
        clientInfoMock.SetupGet(x => x.Name).Returns("my-client");

        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Message)
            .Returns("Validation failed for persisted queries.");
        errorMock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Client)
            .Returns(clientInfoMock.Object);
        errorMock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Queries)
            .Returns(new[] { queryMock.Object });

        SetupPublishClientMutation();
        SetupPublishClientSubscription(
            CreateClientVersionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new client version.
            └── Client 'my-client' (ID: client-1)
                └── Operation 'abc123'
                    └── Field 'foo' does not exist.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Publish_Should_PassWaitForApproval_When_FlagProvided()
    {
        // arrange
        SetupPublishClientMutation(waitForApproval: true);
        SetupPublishClientSubscription(
            CreateClientVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new client version 'v1' to stage 'dev' of client 'client-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new client version 'v1' to stage 'dev'.
            """);
    }

    public static TheoryData<IPublishClientVersion_PublishClient_Errors, string> GetMutationErrors()
    {
        var unexpectedError = new Mock<IPublishClientVersion_PublishClient_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new PublishClientVersion_PublishClient_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                "Not authorized to publish."
            },
            {
                new PublishClientVersion_PublishClient_Errors_ClientNotFoundError(
                    "Client not found.",
                    "client-1"),
                "Client not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    "dev"),
                "Stage not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError(
                    "v1",
                    "Client version not found.",
                    "client-1"),
                "Client version not found."
            },
            {
                new PublishClientVersion_PublishClient_Errors_InvalidSourceMetadataInputError(
                    "InvalidSourceMetadataInputError",
                    "Invalid source metadata."),
                "Invalid source metadata."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
