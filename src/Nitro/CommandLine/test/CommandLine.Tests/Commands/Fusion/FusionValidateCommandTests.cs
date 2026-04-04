using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionValidateCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "validate", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information

            Example:
              nitro fusion validate \
                --api-id "<api-id>" \
                --stage "dev" \
                --source-schema-file ./products/schema.graphqls \
                --source-schema-file ./reviews/schema.graphqls
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

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    #region Option Validation

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--stage' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MultipleExclusiveOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The options '--source-schema-file' and '--archive' are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NeitherArchiveNorSourceSchemaFile_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing one of the required options '--source-schema-file' or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    #region Archive

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithArchive_FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.FusionConfigFile, ArchiveFile);

        SetupValidateArchiveFile();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate");

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id)
            └── ✓ Validation passed.
            """);
    }

    [Fact]
    public async Task WithArchive_ReturnsSuccess()
    {
        // arrange
        SetupValidateArchiveFile();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id)
            └── ✓ Validation passed.
            """);
    }

    [Theory]
    [MemberData(nameof(GetValidateSchemaVersionErrors))]
    public async Task WithArchive_ValidateSchemaVersionHasErrors_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupValidateArchiveFile();
        SetupValidateSchemaVersionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ValidateSchemaVersionThrows_ReturnsError()
    {
        // arrange
        SetupValidateArchiveFile();
        SetupValidateSchemaVersionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupValidateArchiveFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Fusion configuration validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Validation request created (ID: request-id)
            ├── Validating...
            ├── Validating...
            └── ✕ Failed to validate the Fusion configuration.
                ├── Field 'Query.foo' has no type. SCHEMA_ERROR
                ├── Client 'test-client' (ID: client-1)
                │   └── Operation 'abc123'
                ├── OpenAPI collection 'petstore' (ID: collection-1)
                │   └── Endpoint 'GET /pets'
                │       └── Invalid schema. (10:5)
                └── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
                    └── Tool 'test-tool'
                        └── Invalid MCP schema. (5:3)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    #region Source Schema File

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithSourceSchemaFile_FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema file '/some/working/directory/products/schema.graphqls' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created (ID: request-id)
            └── ✓ Validation passed.
            """);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created (ID: request-id)
            └── ✓ Validation passed.
            """);
    }

    [Theory]
    [MemberData(nameof(GetValidateSchemaVersionErrors))]
    public async Task WithSourceSchemaFile_ValidateSchemaVersionHasErrors_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ValidateSchemaVersionThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupValidateSchemaVersionMutation();
        SetupSchemaVersionValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Fusion configuration validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created (ID: request-id)
            ├── Validating...
            ├── Validating...
            └── ✕ Failed to validate the Fusion configuration.
                ├── Field 'Query.foo' has no type. SCHEMA_ERROR
                ├── Client 'test-client' (ID: client-1)
                │   └── Operation 'abc123'
                ├── OpenAPI collection 'petstore' (ID: collection-1)
                │   └── Endpoint 'GET /pets'
                │       └── Invalid schema. (10:5)
                └── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
                    └── Tool 'test-tool'
                        └── Invalid MCP schema. (5:3)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_CompositionErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFileWithInvalidSchema();
        SetupFusionConfigurationDownload();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration against stage 'dev' of API 'api-1'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✕ Failed to compose new Fusion configuration.
            └── ✕ Failed to validate the Fusion configuration.

            ## Composition log

            ❌ [ERR] The @require directive on argument 'Query.field(arg:)' in schema 'products' contains invalid syntax in the 'field' argument. (REQUIRE_INVALID_SYNTAX)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

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
