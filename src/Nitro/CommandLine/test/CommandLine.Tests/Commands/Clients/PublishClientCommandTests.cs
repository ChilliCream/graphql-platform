using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class PublishClientCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultClientId = "client-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1.0";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "client",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a client version to a stage.

            Usage:
              nitro client publish [options]

            Options:
              --tag <tag> (REQUIRED)              The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)          The name of the stage [env: NITRO_STAGE]
              --client-id <client-id> (REQUIRED)  The ID of the client [env: NITRO_CLIENT_ID]
              --force                             Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                 Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
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
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IPublishClientVersion_PublishClient_Errors mutationError,
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IPublishClientVersion_PublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IPublishClientVersion_PublishClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var client = CreatePublishSetup(
            CreatePublishPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishClientVersion_PublishClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_Interactive()
    {
        // arrange
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishClientVersion_PublishClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishClientVersion_PublishClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── The committing of your request is in progress.
            └── ✓ Published new client version 'v1.0' to stage 'production'.

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
    public async Task Subscription_Success_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertSuccessful();

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
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
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── The committing of your request is in progress.
            └── ✕ Failed to publish a new client version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_Interactive()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            Client publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_JsonOutput()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
            {
                CreateOperationInProgress(),
                CreatePublishFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Something went wrong during publish.
            Client publish failed.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── The committing of your request is in progress.
            └── ✕ Failed to publish a new client version.
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
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── Your request is queued. The current position in the queue is 3.
            ├── The committing of your request is in progress.
            └── ✓ Published new client version 'v1.0' to stage 'production'.

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
    public async Task Subscription_ReadyState_PrintsSuccess_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            Your request is ready for the committing.
            ├── The committing of your request is in progress.
            └── ✓ Published new client version 'v1.0' to stage 'production'.

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
    public async Task Subscription_ApprovedState_UpdatesActivity_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── The committing of your request is approved.
            ├── The committing of your request is in progress.
            └── ✓ Published new client version 'v1.0' to stage 'production'.

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
    public async Task Subscription_WaitForApproval_UpdatesActivity_NonInteractive()
    {
        // arrange
        var waitForApprovalEvent = new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── Publish request created (ID: request-1)
            ├── The committing of your request is waiting for approval. Check Nitro to
            approve the request.
            ├── The committing of your request is approved.
            ├── The committing of your request is in progress.
            └── ✓ Published new client version 'v1.0' to stage 'production'.

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
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId)
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
            new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate[]
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
                "client",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--force")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new client version 'v1.0' to stage 'production' of client 'client-1'
            ├── ! Force push is enabled.
            ├── Publish request created (ID: request-1)
            └── ✓ Published new client version 'v1.0' to stage 'production'.

            {
              "stage": "production",
              "status": "success"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static IPublishClientVersion_PublishClient CreateSuccessPayload()
    {
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishClientVersion_PublishClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IPublishClientVersion_PublishClient CreatePublishPayloadWithErrors(
        params IPublishClientVersion_PublishClient_Errors[] errors)
    {
        var payload = new Mock<IPublishClientVersion_PublishClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static Mock<IClientsClient> CreatePublishSetup(
        IPublishClientVersion_PublishClient payload,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                DefaultClientId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        return client;
    }

    private static Mock<IClientsClient> CreatePublishSetupWithSubscription(
        IPublishClientVersion_PublishClient mutationPayload,
        IEnumerable<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate> subscriptionEvents,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                DefaultClientId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToClientPublishAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        return client;
    }

    private static Mock<IClientsClient> CreatePublishExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientPublishAsync(
                DefaultClientId,
                DefaultStage,
                DefaultTag,
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreateOperationInProgress()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreatePublishSuccess()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishSuccess(
            "ClientVersionPublishSuccess",
            ProcessingState.Success);
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreatePublishFailed(
        params IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Errors[] errors)
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ClientVersionPublishFailed(
            "ClientVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreateQueuedEvent(
        int queuePosition)
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreateReadyEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    private static IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate CreateApprovedEvent()
    {
        return new OnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new PublishClientVersion_PublishClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to publish."),
            """
            Not authorized to publish.
            """
        ];

        yield return
        [
            new PublishClientVersion_PublishClient_Errors_ClientNotFoundError(
                "Client not found.",
                DefaultClientId),
            """
            Client not found.
            """
        ];

        yield return
        [
            new PublishClientVersion_PublishClient_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new PublishClientVersion_PublishClient_Errors_ClientVersionNotFoundError(
                DefaultTag,
                "Client version not found.",
                DefaultClientId),
            """
            Client version not found.
            """
        ];

        yield return
        [
            new PublishClientVersion_PublishClient_Errors_InvalidSourceMetadataInputError(
                "Invalid source metadata."),
            """
            Invalid source metadata.
            """
        ];

        var unexpectedError = new Mock<IPublishClientVersion_PublishClient_Errors>();
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
