using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ValidateClientCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultClientId = "client-1";
    private const string DefaultStage = "production";
    private const string DefaultOperationsFile = "operations.graphql";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "client",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a client version.

            Usage:
              nitro client validate [options]

            Options:
              --client-id <client-id> (REQUIRED)              The ID of the client [env: NITRO_CLIENT_ID]
              --stage <stage> (REQUIRED)                      The name of the stage [env: NITRO_STAGE]
              --operations-file <operations-file> (REQUIRED)  The path to the json file with the operations [env: NITRO_OPERATIONS_FILE]
              --cloud-url <cloud-url>                         The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                             The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                 The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                  Show help and usage information

            Example:
              nitro client validate \
                --client-id "<client-id>" \
                --stage "dev" \
                --operations-file ./operations.json
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
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
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
        var client = CreateValidationExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
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
        var client = CreateValidationExceptionClient(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
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
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException());
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
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
        var client = CreateValidationExceptionClient(
            new NitroClientAuthorizationException());
        var fileSystem = CreateOperationsFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
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
        IValidateClientVersion_ValidateClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IValidateClientVersion_ValidateClient_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
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
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateClientVersion_ValidateClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create client validation request.
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
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateClientVersion_ValidateClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns((string?)null);

        var (client, fileSystem) = CreateValidationSetup(payload.Object);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create client validation request.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationInProgress(),
                CreateValidationSuccess()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated client against stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Client validation failed.
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
        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateOperationInProgress()
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the client.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                unknownEvent.Object
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_SubscriptionHasValidationError()
    {
        // arrange
        var queryErrorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>(
            MockBehavior.Strict);
        queryErrorMock.SetupGet(x => x.Message).Returns("Field 'bar' does not exist.");
        queryErrorMock.SetupGet(x => x.Code).Returns("FIELD_NOT_FOUND");
        queryErrorMock.SetupGet(x => x.Path).Returns((string?)null);
        queryErrorMock.SetupGet(x => x.Locations)
            .Returns((IReadOnlyList<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>?)null);

        var queryMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        queryMock.SetupGet(x => x.Message).Returns("Query def456 is invalid.");
        queryMock.SetupGet(x => x.Hash).Returns("def456");
        queryMock.SetupGet(x => x.DeployedTags).Returns(new List<string>());
        queryMock.SetupGet(x => x.Errors).Returns(new[] { queryErrorMock.Object });

        var clientInfoMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(
            MockBehavior.Strict);
        clientInfoMock.SetupGet(x => x.Id).Returns(DefaultClientId);
        clientInfoMock.SetupGet(x => x.Name).Returns("my-client");

        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
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

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the client.
            └── Client 'my-client' (ID: client-1)
                └── Operation 'def456'
                    └── Field 'bar' does not exist.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_SubscriptionHasTimeoutError()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IProcessingTimeoutError>()
            .SetupGet(x => x.Message)
            .Returns("The operation has timed out.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[]
            {
                CreateOperationInProgress(),
                CreateValidationFailed(errorMock.Object)
            });

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'production' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The operation has timed out.
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_SourceMetadataInvalid()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "validate",
                "--stage",
                DefaultStage,
                "--client-id",
                DefaultClientId,
                "--operations-file",
                DefaultOperationsFile,
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

    // --- Helpers ---

    private static Mock<IFileSystem> CreateOperationsFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(DefaultOperationsFile))
            .Returns(new MemoryStream("{}"u8.ToArray()));
        return fileSystem;
    }

    private static IValidateClientVersion_ValidateClient CreateSuccessPayload()
    {
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateClientVersion_ValidateClient_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IValidateClientVersion_ValidateClient CreateValidationPayloadWithErrors(
        params IValidateClientVersion_ValidateClient_Errors[] errors)
    {
        var payload = new Mock<IValidateClientVersion_ValidateClient>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static (Mock<IClientsClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetup(
        IValidateClientVersion_ValidateClient payload)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientValidationAsync(
                DefaultClientId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateOperationsFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IClientsClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithSubscription(
        IValidateClientVersion_ValidateClient mutationPayload,
        IEnumerable<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate> subscriptionEvents)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientValidationAsync(
                DefaultClientId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToClientValidationAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateOperationsFileSystem();

        return (client, fileSystem);
    }

    private static Mock<IClientsClient> CreateValidationExceptionClient(Exception ex)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientValidationAsync(
                DefaultClientId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate CreateOperationInProgress()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate CreateValidationInProgress()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate CreateValidationSuccess()
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationSuccess(
            "ClientVersionValidationSuccess",
            ProcessingState.Success);
    }

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate CreateValidationFailed(
        params IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors[] errors)
    {
        return new OnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationFailed(
            "ClientVersionValidationFailed",
            ProcessingState.Failed,
            errors);
    }

    public static IEnumerable<object[]> MutationErrorCases()
    {
        yield return
        [
            new ValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            """
            Not authorized to validate.
            """
        ];

        yield return
        [
            new ValidateClientVersion_ValidateClient_Errors_ClientNotFoundError(
                "Client not found.",
                DefaultClientId),
            """
            Client not found.
            """
        ];

        yield return
        [
            new ValidateClientVersion_ValidateClient_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new ValidateClientVersion_ValidateClient_Errors_InvalidSourceMetadataInputError(
                "InvalidSourceMetadataInputError",
                "Invalid source metadata."),
            """
            Invalid source metadata.
            """
        ];

        var unexpectedError = new Mock<IValidateClientVersion_ValidateClient_Errors>();
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
