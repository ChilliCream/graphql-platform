using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class PublishOpenApiCollectionCommandTests
{
    private const string DefaultOpenApiCollectionId = "oa-1";
    private const string DefaultStage = "production";
    private const string DefaultTag = "v1";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "openapi",
                "publish",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish an OpenAPI collection version to a stage.

            Usage:
              nitro openapi publish [options]

            Options:
              --tag <tag> (REQUIRED)                                      The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                                  The name of the stage [env: NITRO_STAGE]
              --openapi-collection-id <openapi-collection-id> (REQUIRED)  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              --force                                                     Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                                         Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                              Show help and usage information
            """);
    }

    [Fact]
    public async Task NoSession_Or_ApiKey_ReturnsError_NonInteractive()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Object reference not set to an instance of an object.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoSession_Or_ApiKey_ReturnsError_Interactive()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Object reference not set to an instance of an object.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoSession_Or_ApiKey_ReturnsError_JsonOutput()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Object reference not set to an instance of an object.
            """);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            └── ✕ Failed to publish a new OpenAPI collection version.
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
            """);
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            └── ✕ Failed to publish a new OpenAPI collection version.
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
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
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreatePublishExceptionClient(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
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
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors mutationError,
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors mutationError,
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors mutationError,
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError_NonInteractive()
    {
        // arrange
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            └── ✕ Failed to publish a new OpenAPI collection version.
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
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
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
    public async Task MutationReturnsNullRequestId_ReturnsError_JsonOutput()
    {
        // arrange
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var client = CreatePublishSetup(payload.Object);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Processing...
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'production'.
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
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
        var errorMock = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Processing...
            └── ✕ Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            OpenAPI collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_Interactive()
    {
        // arrange
        var errorMock = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Failed to publish a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during publish.
            OpenAPI collection publish failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_JsonOutput()
    {
        // arrange
        var errorMock = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during publish.");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Something went wrong during publish.
            OpenAPI collection publish failed.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Processing...
            └── ✕ Failed to publish a new OpenAPI collection version.
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Queued at position 3.
            ├── Processing...
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'production'.
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Ready.
            ├── Processing...
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'production'.
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
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Approved. Processing...
            ├── Processing...
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'production'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_WaitForApproval_UpdatesActivity_NonInteractive()
    {
        // arrange
        var waitForApprovalEvent = new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_WaitForApproval(
            "WaitForApproval",
            ProcessingState.WaitingForApproval,
            null);

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
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
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new OpenAPI collection version 'v1' to stage 'production'
            ├── Waiting for approval. Approve in Nitro to continue.
            ├── Approved. Processing...
            ├── Processing...
            └── ✓ Published new OpenAPI collection version 'v1' to stage 'production'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var client = CreatePublishSetupWithSubscription(
            CreateSuccessPayload(),
            new IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "publish",
                "--tag",
                DefaultTag,
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId)
            .ExecuteAsync();

        // assert
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection CreateSuccessPayload()
    {
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection CreatePublishPayloadWithErrors(
        params IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static Mock<IOpenApiClient> CreatePublishSetup(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection payload,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionPublishAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        return client;
    }

    private static Mock<IOpenApiClient> CreatePublishSetupWithSubscription(
        IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection mutationPayload,
        IEnumerable<IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate> subscriptionEvents,
        bool force = false,
        bool waitForApproval = false)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionPublishAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                DefaultTag,
                force,
                waitForApproval,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToOpenApiCollectionPublishAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        return client;
    }

    private static Mock<IOpenApiClient> CreatePublishExceptionClient(Exception ex)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionPublishAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                DefaultTag,
                false,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreateOperationInProgress()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreatePublishSuccess()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishSuccess(
            "OpenApiCollectionVersionPublishSuccess",
            ProcessingState.Success);
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreatePublishFailed(
        params IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_Errors[] errors)
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_OpenApiCollectionVersionPublishFailed(
            "OpenApiCollectionVersionPublishFailed",
            ProcessingState.Failed,
            errors);
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreateQueuedEvent(
        int queuePosition)
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskIsQueued(
            "ProcessingTaskIsQueued",
            "ProcessingTaskIsQueued",
            queuePosition);
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreateReadyEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskIsReady(
            "ProcessingTaskIsReady",
            "ProcessingTaskIsReady");
    }

    private static IPublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate CreateApprovedEvent()
    {
        return new PublishOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionPublishingUpdate_ProcessingTaskApproved(
            "ProcessingTaskApproved",
            ProcessingState.Approved);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to publish."),
            """
            Not authorized to publish.
            """
        ];

        yield return
        [
            new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                DefaultOpenApiCollectionId,
                "OpenAPI collection not found."),
            """
            OpenAPI collection not found.
            """
        ];

        yield return
        [
            new PublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors_OpenApiCollectionVersionNotFoundError(
                DefaultTag,
                "OpenAPI collection version not found.",
                DefaultOpenApiCollectionId),
            """
            OpenAPI collection version not found.
            """
        ];

        var unexpectedError = new Mock<IPublishOpenApiCollectionCommandMutation_PublishOpenApiCollection_Errors>();
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
