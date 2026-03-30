using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class PublishSchemaCommandTests
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "schema",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a schema version to a stage

            Usage:
              nitro schema publish [options]

            Options:
              --tag <tag> (REQUIRED)        The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --force                       Will not ask for confirmation on deletes or overwrites.
              --wait-for-approval           Wait for approval [env: NITRO_SUBGRAPH_NAME]
              --cloud-url <cloud-url>       The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>               The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
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
            new NitroClientException("publish failed"));

        // act
        var result = await new CommandBuilder()
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
            Publishing...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: publish failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientException("publish failed"));

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Publishing...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: publish failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientException("publish failed"));

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            There was an unexpected error executing your request: publish failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
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
            Publishing...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Publishing...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

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
        var result = await new CommandBuilder()
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
            Publishing...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IPublishSchemaVersion_PublishSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Publishing...
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IPublishSchemaVersion_PublishSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(expectedStdErr);

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
        var result = await new CommandBuilder()
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
            Publishing...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create publish request.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Publishing...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create publish request.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            Could not create publish request.
            """);

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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── The committing of your request is in progress.
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "stage": "production",
              "status": "success"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── The committing of your request is in progress.
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Schema publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_Interactive()
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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] The committing of your request is in progress.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Schema publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_JsonOutput()
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
        var result = await new CommandBuilder()
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
        result.AssertError(
            """
            Something went wrong during publish.
            Schema publish failed.
            """);

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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── The committing of your request is in progress.
            └── ✕ Failed!
            """);
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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── Your request is queued. The current position in the queue is 3.
            ├── The committing of your request is in progress.
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
            Publishing...
            Your request is ready for the committing.
            ├── The committing of your request is in progress.
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── The committing of your request is approved.
            ├── The committing of your request is in progress.
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
            Publishing...
            ├── The committing of your request is waiting for approval. Check Nitro to
            approve the request.
            ├── The committing of your request is approved.
            ├── The committing of your request is in progress.
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
        var result = await new CommandBuilder()
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
        var result = await new CommandBuilder()
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
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            LOG: Force push is enabled
            └── ✓ Successfully published schema!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

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
