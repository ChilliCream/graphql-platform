using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ValidateOpenApiCollectionCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultOpenApiCollectionId = "oa-1";
    private const string DefaultStage = "production";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "openapi",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate an OpenAPI collection version.
            
            Usage:
              nitro openapi validate [options]
            
            Options:
              --openapi-collection-id <openapi-collection-id> (REQUIRED)  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              --stage <stage> (REQUIRED)                                  The name of the stage [env: NITRO_STAGE]
              -p, --pattern <pattern> (REQUIRED)                          One or more glob patterns for selecting OpenAPI document files
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
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 0 document(s).
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find any OpenAPI documents with the provided pattern.
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find any OpenAPI documents with the provided pattern.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
        var (client, fileSystem) = CreateValidationSetupWithException(
            new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
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

    [Theory]
    [MemberData(nameof(MutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors mutationError,
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCasesWithModes))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors mutationError,
        string expectedStdErr,
        InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetup(
            CreateValidationPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
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
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>?)null);
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
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
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>?)null);
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated OpenAPI collection against stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
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
        var errorMock = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            OpenAPI collection validation failed.
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
        var errorMock = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            OpenAPI collection validation failed.
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
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── ! Unknown server response. Consider updating the CLI.
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Validate_Should_ReturnError_When_SourceMetadataInvalid(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql",
                "--source-metadata",
                "{broken}")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to parse --source-metadata: 'b' is an invalid start of a property name.
            Expected a '"'. Path: $ | LineNumber: 0 | BytePositionInLine: 1.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_ArchiveValidationError()
    {
        // arrange
        var errorMock = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IOpenApiCollectionValidationArchiveError>()
            .SetupGet(x => x.Message)
            .Returns("Archive is corrupted.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate[]
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
                "openapi",
                "validate",
                "--stage",
                DefaultStage,
                "--openapi-collection-id",
                DefaultOpenApiCollectionId,
                "--pattern",
                "**/*.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'production'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server received an invalid archive. This indicates a bug in the tooling.
            Please notify ChilliCream.
            Error received: Archive is corrupted.
            OpenAPI collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    // --- Helpers ---

    private static readonly byte[] _validOpenApiGraphql =
        """query GetUsers @http(method: GET, route: "/users") { users { id } }"""u8.ToArray();

    private static Mock<IFileSystem> CreateOpenApiFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns(["api.openapi.graphql"]);
        fileSystem.Setup(x => x.ReadAllBytesAsync("api.openapi.graphql", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_validOpenApiGraphql);
        return fileSystem;
    }

    private static IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection CreateSuccessPayload()
    {
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection CreateValidationPayloadWithErrors(
        params IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static (Mock<IOpenApiClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetup(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection payload)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionValidationAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateOpenApiFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IOpenApiClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithSubscription(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection mutationPayload,
        IEnumerable<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate> subscriptionEvents)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionValidationAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToOpenApiCollectionValidationAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateOpenApiFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IOpenApiClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithException(
        Exception ex)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.StartOpenApiCollectionValidationAsync(
                DefaultOpenApiCollectionId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateOpenApiFileSystem();

        return (client, fileSystem);
    }

    private static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate CreateOperationInProgress()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate CreateValidationInProgress()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    private static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate CreateValidationSuccess()
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OpenApiCollectionVersionValidationSuccess(
            "OpenApiCollectionVersionValidationSuccess",
            ProcessingState.Success);
    }

    private static IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate CreateValidationFailed(
        params IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors[] errors)
    {
        return new ValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_OpenApiCollectionVersionValidationFailed(
            "OpenApiCollectionVersionValidationFailed",
            ProcessingState.Failed,
            errors);
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
            new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            """
            Not authorized to validate.
            """
        ];

        yield return
        [
            new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                DefaultOpenApiCollectionId,
                "OpenAPI collection not found."),
            """
            OpenAPI collection not found.
            """
        ];

        var unexpectedError = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>();
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
