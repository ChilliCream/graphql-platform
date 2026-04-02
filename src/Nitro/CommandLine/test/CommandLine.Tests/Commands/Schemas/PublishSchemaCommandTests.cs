using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class PublishSchemaCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "schema",
                "publish",
                "--help")
            .ExecuteAsync();

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
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IPublishSchemaVersion_PublishSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCasesWithModes))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IPublishSchemaVersion_PublishSchema_Errors mutationError,
        string expectedStdErr,
        InteractionMode mode)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsNullRequestId_ReturnsError(InteractionMode mode)
    {
        // arrange
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "stage": "production",
              "status": "success"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
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

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Subscription_FailedWithSimpleError_ReturnsError(InteractionMode mode)
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Schema publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is being processed.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_QueuePosition_UpdatesActivity_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateQueuedEvent(3),
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is queued. The current position in the queue is 3.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ReadyState_PrintsSuccess_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateReadyEvent(),
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is ready for processing.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ApprovedState_UpdatesActivity_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreateApprovedEvent(),
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity_NonInteractive()
    {
        // arrange
        var waitForApprovalEvent = new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                waitForApprovalEvent,
                CreateApprovedEvent(),
                CreateOperationInProgress(),
                CreatePublishSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Your request is waiting for approval. Check Nitro to approve the request.
            │   ├── Your request has been approved.
            │   ├── Your request is being processed.
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId)
            .ExecuteAsync();

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Unknown server response. Ensure your CLI is on the latest version.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ForceOption_LogsForceEnabled_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreatePublishSuccess()
            },
            force: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Publish_Should_ReturnError_When_SourceMetadataInvalid(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--source-metadata",
                "{broken}")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to parse --source-metadata: 'b' is an invalid start of a property name. Expected a '"'. Path: $ | LineNumber: 0 | BytePositionInLine: 1.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Publish_Should_PassWaitForApproval_When_FlagProvided()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate[]
            {
                CreatePublishSuccess()
            },
            waitForApproval: true);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--wait-for-approval")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' to stage 'production' of API 'api-1'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   └── ✓ Published successfully.
            └── ✓ Published new schema version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static IPublishSchemaVersion_PublishSchema CreateSuccessPayload()
    {
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IPublishSchemaVersion_PublishSchema CreatePublishPayloadWithErrors(
        params IPublishSchemaVersion_PublishSchema_Errors[] errors)
    {
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static Mock<ISchemasClient> CreatePublishSetup(
        IPublishSchemaVersion_PublishSchema payload,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaPublishAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        return client;
    }

    private static Mock<ISchemasClient> CreatePublishSetupWithSubscription(
        IPublishSchemaVersion_PublishSchema mutationPayload,
        IEnumerable<IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate> subscriptionEvents,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaPublishAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToSchemaPublishAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        return client;
    }

    private static Mock<ISchemasClient> CreatePublishExceptionClient(Exception ex)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaPublishAsync(
                DefaultApiId,
                DefaultStage,
                DefaultTag,
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreateOperationInProgress()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreatePublishSuccess()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishSuccess(
            "SchemaVersionPublishSuccess",
            ProcessingState.Success);
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreatePublishFailed(
        params IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_Errors[] errors)
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_SchemaVersionPublishFailed(
            "SchemaVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreateQueuedEvent(
        int queuePosition)
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreateReadyEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    private static IOnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate CreateApprovedEvent()
    {
        return new OnSchemaVersionPublishUpdated_OnSchemaVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    public static IEnumerable<object[]> MutationErrorCasesWithModes()
    {
        foreach (var errorCase in MutationErrorCases())
        {
            yield return [.. errorCase, InteractionMode.Interactive];
            yield return [.. errorCase, InteractionMode.JsonOutput];
        }
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new PublishSchemaVersion_PublishSchema_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to publish."),
            """
            Not authorized to publish.
            """
        ];

        yield return
        [
            new PublishSchemaVersion_PublishSchema_Errors_ApiNotFoundError(
                "ApiNotFoundError",
                "API not found.",
                DefaultApiId),
            """
            API not found.
            """
        ];

        yield return
        [
            new PublishSchemaVersion_PublishSchema_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new PublishSchemaVersion_PublishSchema_Errors_SchemaNotFoundError(
                "Schema not found.",
                DefaultApiId,
                DefaultTag),
            """
            Schema not found.
            """
        ];

        var unexpectedError = new Mock<IPublishSchemaVersion_PublishSchema_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        yield return
        [
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """
        ];
    }
}
