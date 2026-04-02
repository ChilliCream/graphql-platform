using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class PublishMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultMcpFeatureCollectionId = "mcp-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mcp",
                "publish",
                "--help")
            .ExecuteAsync();

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
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mcp",
                "publish",
                "--mcp-feature-collection-id",
                DefaultMcpFeatureCollectionId,
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--force",
                "--wait-for-approval")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The '--force' and '--wait-for-approval' options are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoSession_Or_ApiKey_ReturnsError_NonInteractive()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Object reference not set to an instance of an object.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
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
        result.StdErr.MatchInlineSnapshot(
            """
            Object reference not set to an instance of an object.
            """);
        Assert.Equal(1, result.ExitCode);
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
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
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCasesNonInteractive))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors mutationError,
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        InteractionMode mode,
        IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors mutationError,
        string expectedStdErr)
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
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✕ Failed to start publish request.
            └── ✕ Failed to publish a new MCP feature collection version.
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
        var payload = new Mock<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

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
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess();

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
        var result = await new CommandBuilder(fixture)
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
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            MCP Feature Collection publish failed.
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
        var result = await new CommandBuilder(fixture)
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
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            MCP Feature Collection publish failed.
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
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
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Queued at position 3.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

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
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Ready.
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

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
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

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
        var result = await new CommandBuilder(fixture)
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
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

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
        var result = await new CommandBuilder(fixture)
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
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_ForceEnabled_ShowsWarning_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishSuccess()
            },
            force: true);

        // act
        var result = await new CommandBuilder(fixture)
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
                DefaultMcpFeatureCollectionId,
                "--force")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── ! Force push is enabled.
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_WaitForApprovalThenApproved_ReturnsSuccess_NonInteractive()
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
            },
            waitForApproval: true);

        // act
        var result = await new CommandBuilder(fixture)
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
                DefaultMcpFeatureCollectionId,
                "--wait-for-approval")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            │   ├── Processing...
            │   └── ✓ Published successfully.
            └── ✓ Published new MCP feature collection version 'v1' to stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithValidationError_ReturnsError_NonInteractive()
    {
        // arrange
        var validationError = new Mock<IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        validationError.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(Array.Empty<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections>());

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(validationError.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            MCP Feature Collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithTimeoutError_ReturnsError_NonInteractive()
    {
        // arrange
        var timeoutError = new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors_ProcessingTimeoutError(
            "ProcessingTimeoutError",
            "The operation timed out.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(timeoutError)
            });

        // act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The operation timed out.
            MCP Feature Collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithConcurrentOpError_ReturnsError_NonInteractive()
    {
        // arrange
        var concurrentError = new PublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate_Errors_ConcurrentOperationError(
            "ConcurrentOperationError",
            "A concurrent operation is in progress.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(concurrentError)
            });

        // act
        var result = await new CommandBuilder(fixture)
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
            Publishing new MCP feature collection version 'v1' to stage 'production'
            ├── Starting publish request
            │   └── ✓ Publish request created (ID: request-1).
            ├── Processing
            │   ├── Processing...
            │   └── ✕ Processing failed.
            └── ✕ Failed to publish a new MCP feature collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            A concurrent operation is in progress.
            MCP Feature Collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishMcpFeatureCollectionCommandSubscription_OnMcpFeatureCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
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
        Assert.Empty(result.StdErr);
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
        var modes = new[] { InteractionMode.Interactive, InteractionMode.JsonOutput };

        foreach (var mode in modes)
        {
            yield return
            [
                mode,
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to publish."),
                """
                Not authorized to publish.
                """
            ];

            yield return
            [
                mode,
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
                mode,
                new PublishMcpFeatureCollectionCommandMutation_PublishMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    DefaultMcpFeatureCollectionId,
                    "MCP Feature Collection not found."),
                """
                MCP Feature Collection not found.
                """
            ];

            yield return
            [
                mode,
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
                mode,
                unexpectedError.Object,
                """
                Unexpected mutation error: Something went wrong.
                """
            ];
        }
    }

    public static IEnumerable<object[]> MutationErrorCasesNonInteractive()
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
