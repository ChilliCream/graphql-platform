using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ValidateOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--help");

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

            Example:
              nitro openapi validate \
                --openapi-collection-id "<collection-id>" \
                --stage "dev" \
                --pattern "./**/*.graphql"
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
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task ValidateOpenApiCollectionThrows_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidateOpenApiCollectionErrors))]
    public async Task ValidateOpenApiCollectionHasErrors_ReturnsError(
        IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✕ Failed to start the validation request.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ValidateOpenApiCollectionReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutationNullRequestId();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
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
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupOpenApiDocument();
        var capturedStream = SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationOperationInProgressEvent(),
            CreateOpenApiCollectionValidationInProgressEvent(),
            CreateOpenApiCollectionValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        Assert.True(capturedStream.Length > 0);
        result.AssertSuccess(
            """
            Validating OpenAPI collection against stage 'dev'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   ├── Validating...
            │   └── ✓ Validation passed.
            └── ✓ Validated OpenAPI collection against stage 'dev'.
            """);
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupOpenApiDocument();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationOperationInProgressEvent(),
            CreateOpenApiCollectionValidationInProgressEvent(),
            CreateOpenApiCollectionValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Subscription_InProgressThenSuccess_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupOpenApiDocument();
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationOperationInProgressEvent(),
            CreateOpenApiCollectionValidationInProgressEvent(),
            CreateOpenApiCollectionValidationSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.AssertSuccess(
            """
            {}
            """);
    }

    [Fact]
    public async Task Subscription_FailedWithSimpleError_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        var errorMock = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong during validation.");

        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationOperationInProgressEvent(),
            CreateOpenApiCollectionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   ├── Validating...
            │   └── ✕ Validation failed.
            │       └── Something went wrong during validation.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            OpenAPI collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Subscription_InProgressOnly_StreamEnds_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationOperationInProgressEvent());

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
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
    }

    [Fact]
    public async Task Subscription_UnknownEvent_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        var unknownEvent = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate>(
            MockBehavior.Strict);
        unknownEvent.SetupGet(x => x.__typename).Returns("UnknownType");

        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(unknownEvent.Object);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        // Falls through the loop with no terminal state, so activity.Fail() is called
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
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
    }

    [Fact]
    public async Task Validate_Should_ReturnError_When_ArchiveValidationError()
    {
        // arrange
        SetupOpenApiDocument();
        var errorMock = new Mock<IValidateOpenApiCollectionCommandSubscription_OnOpenApiCollectionVersionValidationUpdate_Errors>(
            MockBehavior.Strict);
        errorMock.As<IOpenApiCollectionValidationArchiveError>()
            .SetupGet(x => x.Message)
            .Returns("Archive is corrupted.");

        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationFailedEvent(errorMock.Object));

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Validating OpenAPI collection against stage 'dev'
            ├── Found 1 document(s).
            ├── Starting validation request
            │   └── ✓ Validation request created (ID: request-1).
            ├── Validating
            │   └── ✕ Validation failed.
            │       └── The server received an invalid archive. This indicates a bug in the tooling.
            │           Please notify ChilliCream. Error received: Archive is corrupted.
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            OpenAPI collection validation failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    public static TheoryData<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors, string>
        GetValidateOpenApiCollectionErrors()
    {
        var data = new TheoryData<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors, string>
        {
            {
                new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation",
                    "Not authorized to validate."),
                """
                Not authorized to validate.
                """
            },
            {
                new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_StageNotFoundError(
                    "StageNotFoundError",
                    "Stage not found.",
                    Stage),
                """
                Stage not found.
                """
            },
            {
                new ValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                    OpenApiCollectionId,
                    "OpenAPI collection not found."),
                """
                OpenAPI collection not found.
                """
            }
        };

        var unexpectedError = new Mock<IValidateOpenApiCollectionCommandMutation_ValidateOpenApiCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        data.Add(
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """);

        return data;
    }
}
