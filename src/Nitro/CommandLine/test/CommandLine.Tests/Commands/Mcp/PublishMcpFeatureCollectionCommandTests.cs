using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class PublishMcpFeatureCollectionCommandTests
{
    private const string DefaultMcpFeatureCollectionId = "mcp-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "mcp",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish an MCP Feature Collection version to a stage

            Usage:
              nitro mcp publish [options]

            Options:
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                                          The name of the stage [env: NITRO_STAGE]
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --force                                                             Will not ask for confirmation on deletes or overwrites.
              --wait-for-approval                                                 Wait for approval [env: NITRO_SUBGRAPH_NAME]
              --cloud-url <cloud-url>                                             The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>                                                     The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.NotEmpty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientException("publish failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Contains("publish failed", result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Contains(
            "The server rejected your request as unauthorized.",
            result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors mutationError,
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
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
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors mutationError,
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
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
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors mutationError,
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MutationReturnsNullRequestId_ReturnsError(InteractionMode mode)
    {
        // arrange
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Contains("Could not create publish request.", result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            ├── Your request is in progress.
            └── ✓ Successfully published MCP Feature Collection!
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Successfully published MCP Feature Collection!
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {}
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            ├── Your request is in progress.
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            MCP Feature Collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_Interactive()
    {
        // arrange
        var errorMock = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Your request is in progress.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            MCP Feature Collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_JsonOutput()
    {
        // arrange
        var errorMock = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Something went wrong during publish.
            MCP Feature Collection publish failed.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            ├── Your request is in progress.
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            ├── Your request is queued. The current position in the queue is 3.
            ├── Your request is in progress.
            └── ✓ Successfully published MCP Feature Collection!
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Contains("Your request is ready for processing.", result.StdOut);
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing...
            ├── The processing of your request is approved.
            ├── Your request is in progress.
            └── ✓ Successfully published MCP Feature Collection!
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity_NonInteractive()
    {
        // arrange
        var waitForApprovalEvent = new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Contains("waiting for approval", result.StdOut);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mcp",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection CreateSuccessPayload()
    {
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection CreatePublishPayloadWithErrors(
        params IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors[] errors)
    {
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static Mock<IMcpClient> CreatePublishSetup(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection payload,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        return client;
    }

    private static Mock<IMcpClient> CreatePublishSetupWithSubscription(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection mutationPayload,
        IEnumerable<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate> subscriptionEvents,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToMcpFeatureCollectionPublishAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        return client;
    }

    private static Mock<IMcpClient> CreatePublishExceptionClient(Exception ex)
    {
        var client = new Mock<IMcpClient>(MockBehavior.Strict);
        client.Setup(x => x.StartMcpFeatureCollectionPublishAsync(
                DefaultMcpFeatureCollectionId,
                DefaultStage,
                DefaultTag,
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreateOperationInProgress()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreatePublishSuccess()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishSuccess(
            "McpFeatureCollectionVersionPublishSuccess",
            ProcessingState.Success);
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreatePublishFailed(
        params IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors[] errors)
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_McpFeatureCollectionVersionPublishFailed(
            "McpFeatureCollectionVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreateQueuedEvent(
        int queuePosition)
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreateReadyEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    private static IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate CreateApprovedEvent()
    {
        return new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to publish."),
            """
            Not authorized to publish.
            """
        ];

        yield return
        [
            new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                DefaultMcpFeatureCollectionId,
                "MCP Feature Collection not found."),
            """
            MCP Feature Collection not found.
            """
        ];

        yield return
        [
            new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionVersionNotFoundError(
                DefaultTag,
                "MCP Feature Collection version not found.",
                DefaultMcpFeatureCollectionId),
            """
            MCP Feature Collection version not found.
            """
        ];

        var unexpectedError = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>();
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
