using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ValidateClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--help");

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
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task OperationsFileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
            "--operations-file",
            "nonexistent.json");

        // assert
        result.AssertError(
            """
            Operations file '/some/working/directory/nonexistent.json' does not exist.
            """);
    }

    [Fact]
    public async Task StartClientValidationThrows_ReturnsError()
    {
        // arrange
        SetupValidateClientMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetStartClientValidationErrors))]
    public async Task StartClientValidationHasErrors_ReturnsError(
        IValidateClientVersion_ValidateClient_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupValidateClientMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task StartClientValidationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupValidateClientMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create client validation request.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the client.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationOperationInProgressEvent(),
            CreateClientVersionValidationInProgressEvent(),
            CreateClientVersionValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.AssertSuccess(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated client against stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        var errorMock = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationOperationInProgressEvent(),
            CreateClientVersionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            │       └── Something went wrong during validation.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the client.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        var unknownEvent = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupValidateClientMutation();
        SetupValidateClientSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
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
        clientInfoMock.SetupGet(x => x.Id).Returns(ClientId);
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

        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✕ Validation failed.
            │       └── Client 'my-client' (ID: client-1)
            │           └── Operation 'def456'
            │               └── Field 'bar' does not exist.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
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

        SetupValidateClientMutation();
        SetupValidateClientSubscription(
            CreateClientVersionValidationOperationInProgressEvent(),
            CreateClientVersionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "validate",
            "--stage",
            Stage,
            "--client-id",
            ClientId,
            "--operations-file",
            OperationsFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating client against stage 'dev' of client 'client-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            │       └── The operation has timed out.
            └── ✕ Failed to validate the client.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Client validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IValidateClientVersion_ValidateClient_Errors, string>
        GetStartClientValidationErrors() => new()
    {
        {
            new ValidateClientVersion_ValidateClient_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            "Not authorized to validate."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_ClientNotFoundError(
                "Client not found.",
                "client-1"),
            "Client not found."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                "dev"),
            "Stage not found."
        },
        {
            new ValidateClientVersion_ValidateClient_Errors_InvalidSourceMetadataInputError(
                "InvalidSourceMetadataInputError",
                "Invalid source metadata."),
            "Invalid source metadata."
        }
    };
}
