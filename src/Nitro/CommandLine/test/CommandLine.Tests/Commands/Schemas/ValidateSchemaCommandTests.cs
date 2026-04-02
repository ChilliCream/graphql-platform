using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;
using static ChilliCream.Nitro.CommandLine.Tests.TestHelpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class ValidateSchemaCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    private const string DefaultApiId = "api-1";
    private const string DefaultStage = "production";
    private const string DefaultSchemaFile = "schema.graphql";
    private const string DefaultRequestId = "request-1";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "schema",
                "validate",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a schema against a stage.
            
            Usage:
              nitro schema validate [options]
            
            Options:
              --api-id <api-id> (REQUIRED)            The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)              The name of the stage [env: NITRO_STAGE]
              --schema-file <schema-file> (REQUIRED)  The path to the graphql file with the schema definition [env: NITRO_SCHEMA_FILE]
              --cloud-url <cloud-url>                 The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                     The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                          Show help and usage information

            Example:
              nitro schema validate \
                --api-id "<api-id>" \
                --stage "dev" \
                --schema-file ./schema.graphqls
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
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the schema.
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
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the schema.
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
        var fileSystem = CreateSchemaFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the schema.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(MutationErrorCasesWithModes))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors mutationError,
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the schema.
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
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated schema against stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
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
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "requestId": "request-1",
              "status": "success"
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError_NonInteractive()
    {
        // arrange
        var errorMock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Schema validation failed.
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
        var errorMock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Something went wrong during validation.
            Schema validation failed.
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
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the schema.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError_NonInteractive()
    {
        // arrange
        var unknownEvent = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        var (client, fileSystem) = CreateValidationSetupWithSubscription(
            CreateSuccessPayload(),
            new IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[]
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile)
            .ExecuteAsync();

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── ! Warning: Received an unknown server response. Ensure your CLI is on the latest version.
            │   └── ✕ Validation failed.
            └── ✕ Failed to validate the schema.
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
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                DefaultSchemaFile,
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
    public async Task Validate_Should_ReturnError_When_SchemaFileNotFound()
    {
        // arrange
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream("nonexistent.graphql"))
            .Throws(new FileNotFoundException("Could not find file 'nonexistent.graphql'."));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "validate",
                "--stage",
                DefaultStage,
                "--api-id",
                DefaultApiId,
                "--schema-file",
                "nonexistent.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'production' of API 'api-1'
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find file 'nonexistent.graphql'.
            """);
        Assert.Equal(1, result.ExitCode);

        fileSystem.VerifyAll();
    }

    // --- Helpers ---

    private static Mock<IFileSystem> CreateSchemaFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.OpenReadStream(DefaultSchemaFile))
            .Returns(new MemoryStream("type Query { hello: String }"u8.ToArray()));
        return fileSystem;
    }

    private static IValidateSchemaVersion_ValidateSchema CreateSuccessPayload()
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
        payload.SetupGet(x => x.Id)
            .Returns(DefaultRequestId);
        return payload.Object;
    }

    private static IValidateSchemaVersion_ValidateSchema CreateValidationPayloadWithErrors(
        params IValidateSchemaVersion_ValidateSchema_Errors[] errors)
    {
        var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.Id).Returns((string?)null);
        return payload.Object;
    }

    private static (Mock<ISchemasClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetup(
        IValidateSchemaVersion_ValidateSchema payload)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaValidationAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateSchemaFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<ISchemasClient> Client, Mock<IFileSystem> FileSystem) CreateValidationSetupWithSubscription(
        IValidateSchemaVersion_ValidateSchema mutationPayload,
        IEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate> subscriptionEvents)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaValidationAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationPayload);

        client.Setup(x => x.SubscribeToSchemaValidationAsync(
                DefaultRequestId,
                It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken ct) =>
                ToAsyncEnumerable(subscriptionEvents, ct));

        var fileSystem = CreateSchemaFileSystem();

        return (client, fileSystem);
    }

    private static Mock<ISchemasClient> CreateValidationExceptionClient(Exception ex)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaValidationAsync(
                DefaultApiId,
                DefaultStage,
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateOperationInProgress()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_OperationInProgress(
            "OperationInProgress",
            ProcessingState.Processing);
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationInProgress()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress(
            "ValidationInProgress",
            ProcessingState.Processing);
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationSuccess()
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess(
            "SchemaVersionValidationSuccess",
            ProcessingState.Success,
            Array.Empty<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Changes>());
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate CreateValidationFailed(
        params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors[] errors)
    {
        return new OnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed(
            "SchemaVersionValidationFailed",
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
            new ValidateSchemaVersion_ValidateSchema_Errors_UnauthorizedOperation(
                "UnauthorizedOperation",
                "Not authorized to validate."),
            """
            Not authorized to validate.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_ApiNotFoundError(
                "ApiNotFoundError",
                "API not found.",
                DefaultApiId),
            """
            API not found.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_StageNotFoundError(
                "StageNotFoundError",
                "Stage not found.",
                DefaultStage),
            """
            Stage not found.
            """
        ];

        yield return
        [
            new ValidateSchemaVersion_ValidateSchema_Errors_SchemaNotFoundError(
                "Schema not found.",
                DefaultApiId,
                "v1"),
            """
            Schema not found.
            """
        ];

        var unexpectedError = new Mock<IValidateSchemaVersion_ValidateSchema_Errors>();
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
