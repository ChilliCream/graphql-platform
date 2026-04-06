using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class PublishSchemaCommandTests(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a schema version to a stage.

            Usage:
              nitro schema publish [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)        The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --force                       Skip confirmation prompts for deletes and overwrites
              --wait-for-approval           Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro schema publish \
                --api-id "<api-id>" \
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
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--api-id",
            ApiId,
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
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task PublishSchemaThrows_ReturnsError()
    {
        // arrange
        SetupPublishSchemaMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetPublishSchemaErrors))]
    public async Task PublishSchemaHasErrors_ReturnsError(
        IPublishSchemaVersion_PublishSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupPublishSchemaMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SchemasClientMock
            .Setup(x => x.StartSchemaPublishAsync(
                ApiId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
                payload.SetupGet(x => x.Errors)
                    .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
                payload.SetupGet(x => x.Id)
                    .Returns((string?)null);
                return payload.Object;
            });

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Schema publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_QueuePosition_UpdatesActivity()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishQueuedEvent(3),
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is queued. The current position in the queue is 3.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ReadyState_PrintsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishReadyEvent(),
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is ready for processing.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ApprovedState_UpdatesActivity()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishApprovedEvent(),
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishWaitForApprovalEvent(),
            CreateSchemaVersionPublishApprovedEvent(),
            CreateSchemaVersionPublishOperationInProgressEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── Your request is waiting for approval. Check Nitro to approve the request.
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   ├── ! Unknown server response. Ensure your CLI is on the latest version.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ForceOption_LogsForceEnabled()
    {
        // arrange
        SetupPublishSchemaMutation(force: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Publish_Should_PassWaitForApproval_When_FlagProvided()
    {
        // arrange
        SetupPublishSchemaMutation(waitForApproval: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'dev' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-id).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    #region Error Theory Data

    public static TheoryData<
        IPublishSchemaVersion_PublishSchema_Errors,
        string> GetPublishSchemaErrors() => new()
    {
        { CreatePublishSchemaUnauthorizedError(), "Unauthorized." },
        { CreatePublishSchemaApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreatePublishSchemaStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreatePublishSchemaSchemaNotFoundError(), "Schema not found." }
    };

    #endregion
}
