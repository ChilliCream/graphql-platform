using ChilliCream.Nitro.Client;
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
              --openapi-collection-id <openapi-collection-id>  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              --stage <stage>                                  The name of the stage [env: NITRO_STAGE]
              -p, --pattern <pattern> (REQUIRED)               One or more glob patterns for selecting OpenAPI document files
              --cloud-url <cloud-url>                          The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>                              The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                   Show help and usage information

            Example:
              nitro openapi validate \
                --openapi-collection-id "<collection-id>" \
                --stage "dev" \
                --pattern "./**/*.graphql"
            """);
    }

    [Theory]
    [InlineData("--openapi-collection-id")]
    [InlineData("--stage")]
    public async Task MissingRequiredOption_NonInteractive_ReturnsError(string missingOption)
    {
        // arrange
        SetupInteractionMode(InteractionMode.NonInteractive);
        SetupOpenApiDocument();

        var args = new List<string>
        {
            "openapi",
            "validate",
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--stage",
            Stage,
            "--pattern",
            "**/*.graphql"
        };

        var index = args.IndexOf(missingOption);
        args.RemoveRange(index, 2);

        // act
        var result = await ExecuteCommandAsync(args.ToArray());

        // assert
        result.AssertError($"Missing required option '{missingOption}'.");
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
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
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
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
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
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
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
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
            └── ✕ Failed to validate the OpenAPI collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not create validation request!
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupOpenApiDocument();
        var capturedStream = SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription();

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
        await AssertOpenApiCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
            ├── Validation request created. (ID: request-1)
            └── ✓ OpenAPI collection passed validation.
            """);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupOpenApiDocument();
        SetupEnvironmentVariable(EnvironmentVariables.OpenApiCollectionId, OpenApiCollectionId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        var capturedStream = SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "validate",
            "--pattern",
            "**/*.graphql");

        // assert
        await AssertOpenApiCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
            ├── Validation request created. (ID: request-1)
            └── ✓ OpenAPI collection passed validation.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription(
            CreateOpenApiCollectionValidationFailedEventWithErrors());

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
            Validating OpenAPI collection 'oa-1' against stage 'dev'
            ├── Found 1 document(s).
            ├── Validation request created. (ID: request-1)
            └── ✕ OpenAPI collection failed validation.
                └── OpenAPI collection 'petstore' (ID: collection-1)
                    └── Endpoint 'GET /fail'
                        └── The field `person` does not exist on the type `Query`. (1:14)
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            OpenAPI collection failed validation.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Validate_Should_PromptForStage_When_CollectionProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupOpenApiDocument();
        SetupGetOpenApiCollectionApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription();

        var command = StartInteractiveCommand(
            "openapi",
            "validate",
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "**/*.graphql");

        // act
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Validate_Should_PromptForApiAndCollection_When_StageProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListOpenApiCollectionsForPrompt((OpenApiCollectionId, OpenApiCollectionName));
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "openapi",
            "validate",
            "--stage",
            Stage,
            "--pattern",
            "**/*.graphql");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select collection
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Validate_Should_PromptForApiCollectionAndStage_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListOpenApiCollectionsForPrompt((OpenApiCollectionId, OpenApiCollectionName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupOpenApiDocument();
        SetupValidateOpenApiCollectionMutation();
        SetupValidateOpenApiCollectionSubscription();

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "openapi",
            "validate",
            "--pattern",
            "**/*.graphql");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select collection
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
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
