using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class PublishOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish an OpenAPI collection version to a stage.

            Usage:
              nitro openapi publish [options]

            Options:
              --openapi-collection-id <openapi-collection-id> (REQUIRED)  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                      The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                                  The name of the stage [env: NITRO_STAGE]
              --force                                                     Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                                         Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                              Show help and usage information

            Example:
              nitro openapi publish \
                --openapi-collection-id "<collection-id>" \
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
            "openapi",
            "publish",
            "--openapi-collection-id",
            OpenApiCollectionId,
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
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task PublishOpenApiCollectionThrows_ReturnsError()
    {
        // arrange
        SetupPublishOpenApiCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetPublishOpenApiCollectionErrors))]
    public async Task PublishOpenApiCollectionHasErrors_ReturnsError(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupPublishOpenApiCollectionMutation(errors: mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task PublishOpenApiCollectionReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupPublishOpenApiCollectionMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            {}
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            │       └── Something went wrong during publish.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            OpenAPI collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_QueuePosition_UpdatesActivity()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishQueuedEvent(3),
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Queued at position 3.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ReadyState_PrintsSuccess()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishReadyEvent(),
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Ready.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_ApprovedState_UpdatesActivity()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishApprovedEvent(),
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishWaitForApprovalEvent(),
            CreateOpenApiCollectionPublishApprovedEvent(),
            CreateOpenApiCollectionPublishOperationInProgressEvent(),
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── 🕐 Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupPublishOpenApiCollectionMutation();
        SetupPublishOpenApiCollectionSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId);

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Publish_Should_LogWarning_When_ForceEnabled()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation(force: true);
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task Publish_Should_PassWaitForApproval_When_FlagProvided()
    {
        // arrange
        SetupPublishOpenApiCollectionMutation(waitForApproval: true);
        SetupPublishOpenApiCollectionSubscription(
            CreateOpenApiCollectionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'dev'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'dev'.
            """);
    }

    public static TheoryData<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors, string>
        GetPublishOpenApiCollectionErrors()
    {
        var data = new TheoryData<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors, string>
        {
            {
                new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                """
                Not authorized to publish.
                """
            },
            {
                new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    Stage),
                """
                Stage not found.
                """
            },
            {
                new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                    OpenApiCollectionId,
                    "OpenAPI collection not found."),
                """
                OpenAPI collection not found.
                """
            },
            {
                new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionVersionNotFoundError(
                    Tag,
                    "OpenAPI collection version not found.",
                    OpenApiCollectionId),
                """
                OpenAPI collection version not found.
                """
            }
        };

        var unexpectedError = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        data.Add(
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """);

        return data;
    }
}
