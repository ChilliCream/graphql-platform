using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class ValidateSchemaCommandTests(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--help");

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
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task SchemaFileDoesNotExist_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            "nonexistent.graphql");

        // assert
        result.AssertError(
            """
            Schema file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Fact]
    public async Task ValidateSchemaThrows_ReturnsError()
    {
        // arrange
        SetupSchemaFile();
        SetupSchemaValidationMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            └── ✕ Failed to validate the schema.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidateSchemaVersionErrors))]
    public async Task ValidateSchemaVersionHasErrors_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSchemaFile();
        SetupSchemaValidationMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            └── ✕ Failed to validate the schema.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupSchemaFile();

        SchemasClientMock
            .Setup(x => x.StartSchemaValidationAsync(
                ApiId,
                Stage,
                It.IsAny<Stream>(),
                It.IsAny<SourceMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var payload = new Mock<IValidateSchemaVersion_ValidateSchema>(MockBehavior.Strict);
                payload.SetupGet(x => x.Errors)
                    .Returns((IReadOnlyList<IValidateSchemaVersion_ValidateSchema_Errors>?)null);
                payload.SetupGet(x => x.Id)
                    .Returns((string?)null);
                return payload.Object;
            });

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            └── ✕ Failed to validate the schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        SetupSchemaFile();
        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.AssertSuccess(
            """
            Validating schema against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id).
            ├── Validating...
            ├── Validating...
            └── ✓ Validation passed.
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        SetupSchemaFile();

        var errorMock = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id).
            ├── Validating...
            └── ✕ Failed to validate the schema.
                └── Something went wrong during validation.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupSchemaFile();
        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id).
            ├── Validating...
            └── ✕ Failed to validate the schema.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        SetupSchemaFile();

        var unknownEvent = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id).
            ├── ! Unknown server response. Consider updating the CLI.
            └── ✕ Failed to validate the schema.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupSchemaFile();
        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "validate",
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--schema-file",
            SchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating schema against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id).
            ├── Validating...
            ├── Validating...
            └── ✕ Failed to validate the schema.
                ├── Field 'Query.foo' has no type. SCHEMA_ERROR
                ├── Client 'test-client' (ID: client-1)
                │   └── Operation 'abc123'
                ├── OpenAPI collection 'petstore' (ID: collection-1)
                │   └── Endpoint 'GET /pets'
                │       └── Invalid schema. (10:5)
                ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
                │   └── Tool 'test-tool'
                │       └── Invalid MCP schema. (5:3)
                └── An unexpected error occurred.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #region Error Theory Data

    public static TheoryData<
        IValidateSchemaVersion_ValidateSchema_Errors,
        string> GetValidateSchemaVersionErrors() => new()
    {
        { CreateValidateSchemaVersionUnauthorizedError(), "Unauthorized." },
        { CreateValidateSchemaVersionApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreateValidateSchemaVersionStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreateValidateSchemaVersionSchemaNotFoundError(), "Schema not found." }
    };

    #endregion
}
