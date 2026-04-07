using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionPublishCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "publish", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a Fusion configuration to a stage.

            Usage:
              nitro fusion publish [command] [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)                         The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -s, --source-schema <source-schema>            One or more source schemas that should be included in the composition. Source schemas can either be just a name ('example') or a name and a version ('example@1.0.0'). If no version is specified the value of the '--tag' option is taken as the source schema version.
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --force                                        Skip confirmation prompts for deletes and overwrites
              --wait-for-approval                            Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              -w, --working-directory <working-directory>    Set the working directory for the command
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information

            Commands:
              begin     Begin a configuration publish. This command will request a deployment slot.
              start     Start a Fusion configuration publish.
              validate  Validate a Fusion configuration against the schema and clients.
              cancel    Cancel a Fusion configuration publish.
              commit    Commit a Fusion configuration publish.

            Example:
              nitro fusion publish \
                --api-id "<api-id>" \
                --stage "dev" \
                --tag "v1" \
                --source-schema products \
                --source-schema reviews
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
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
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
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--tag' is required.
            Option '--stage' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task No_Archive_Or_SourceSchemaFile_Or_SourceSchema_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing one of the required options '--source-schema', '--source-schema-file', or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Archive_And_SourceSchemaFile_And_SourceSchema_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile,
            "--source-schema-file",
            SourceSchemaFile,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The options '--source-schema', '--source-schema-file', and '--archive' are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WaitForApproval_And_Force_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema,
            "--wait-for-approval",
            "--force");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The '--force' and '--wait-for-approval' options are mutually exclusive.
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
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
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
    public async Task WithArchive_ReturnsSuccess()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task WithArchive_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);
        SetupEnvironmentVariable(EnvironmentVariables.FusionConfigFile, ArchiveFile);

        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync("fusion", "publish");

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Theory]
    [MemberData(nameof(GetRequestDeploymentSlotErrors))]
    public async Task WithArchive_RequestDeploymentSlotHasErrors_ReturnsError(
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: false, errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_RequestDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetClaimDeploymentSlotErrors))]
    public async Task WithArchive_ClaimDeploymentSlotHasError_ReturnsError(
        IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ClaimDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidationErrors))]
    public async Task WithArchive_ValidationHasErrors_ReturnsError(
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ValidationThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_WaitForApproval_NoBreakingChanges_ReturnsSuccess()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupReleaseDeploymentSlotMutation();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task WithArchive_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to validate configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--force",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── ! Force push is enabled.
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task WithArchive_WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreateProcessingTaskApprovedEvent(),
            CreatePublishingSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task WithArchive_WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupReleaseDeploymentSlotMutation();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreatePublishingFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadErrors))]
    public async Task WithArchive_UploadHasErrors_ReturnsError(
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_UploadThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_PublishingFailedWithErrors_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(CreatePublishingFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       └── An error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ReleaseDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Encountered an unexpected exception while trying to release the deployment slot after an error during the publishing process:
            Something unexpected happened.
            This is the error that caused the publishing process to fail in the first place:
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetReleaseDeploymentSlotErrors))]
    public async Task WithArchive_ReleaseDeploymentSlotHasErrors_ReturnsError(
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             Encountered the following errors while trying to release the deployment slot after an error during the publishing process:
             {expectedErrorMessage}
             This is the error that caused the publishing process to fail in the first place:
             There was an unexpected error: Something unexpected happened.
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
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
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Schema file '/some/working/directory/products/schema.graphqls' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);

        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Theory]
    [MemberData(nameof(GetRequestDeploymentSlotErrors))]
    public async Task WithSourceSchemaFile_RequestDeploymentSlotHasErrors_ReturnsError(
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: false, errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_RequestDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetClaimDeploymentSlotErrors))]
    public async Task WithSourceSchemaFile_ClaimDeploymentSlotHasError_ReturnsError(
        IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ClaimDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ConfigurationDownloadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownloadException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✕ Failed to download the existing Fusion configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_CompositionErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFileWithInvalidSchema();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✕ Failed to compose new configuration.
            └── ✕ Failed to publish Fusion configuration.

            ## Composition log

            ❌ [ERR] The @require directive on argument 'Query.field(arg:)' in schema 'products' contains invalid syntax in the 'field' argument. (REQUIRE_INVALID_SYNTAX)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidationErrors))]
    public async Task WithSourceSchemaFile_ValidationHasErrors_ReturnsError(
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ValidationThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);
        ;

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to validate configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--force",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── ! Force push is enabled.
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WaitForApproval_NoBreakingChanges_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreateProcessingTaskApprovedEvent(),
            CreatePublishingSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation(waitForApproval: true);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreatePublishingFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadErrors))]
    public async Task WithSourceSchemaFile_UploadHasErrors_ReturnsError(
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_UploadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_PublishingFailedWithErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(CreatePublishingFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       └── An error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ReleaseDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Encountered an unexpected exception while trying to release the deployment slot after an error during the publishing process:
            Something unexpected happened.
            This is the error that caused the publishing process to fail in the first place:
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetReleaseDeploymentSlotErrors))]
    public async Task WithSourceSchemaFile_ReleaseDeploymentSlotHasErrors_ReturnsError(
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             Encountered the following errors while trying to release the deployment slot after an error during the publishing process:
             {expectedErrorMessage}
             This is the error that caused the publishing process to fail in the first place:
             There was an unexpected error: Something unexpected happened.
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    #region Source Schema

    [Fact]
    public async Task WithSourceSchema_SourceSchemaDoesNotExist_ReturnsError()
    {
        // arrange
        SetupMissingSourceSchemaDownload();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find source schema 'products' with version 'v1'.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✕ Could not find source schema 'products' with version 'v1'.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_SourceSchemaDownloadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownloadException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✕ Failed to download source schemas.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchema_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);

        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--source-schema",
            SourceSchema);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchema_WithExplicitSourceSchemaVersion_ReturnsSuccess()
    {
        // arrange
        const string sourceSchemaVersion = "1.2.3";
        SetupSourceSchemaDownload(version: sourceSchemaVersion);
        SetupRequestDeploymentSlotMutation(
            sourceSchemaVersions: [new SourceSchemaVersion(SourceSchema, sourceSchemaVersion)]);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema + "@" + sourceSchemaVersion);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Theory]
    [MemberData(nameof(GetRequestDeploymentSlotErrors))]
    public async Task WithSourceSchema_RequestDeploymentSlotHasErrors_ReturnsError(
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(waitForApproval: false, sourceSchemaVersions: SourceSchemaVersions, errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_RequestDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✕ Failed to request a deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetClaimDeploymentSlotErrors))]
    public async Task WithSourceSchema_ClaimDeploymentSlotHasError_ReturnsError(
        IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_ClaimDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_CompositionErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownloadWithInvalidSchema();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✕ Failed to compose new configuration.
            └── ✕ Failed to publish Fusion configuration.

            ## Composition log

            ❌ [ERR] The @require directive on argument 'Query.field(arg:)' in schema 'products' contains invalid syntax in the 'field' argument. (REQUIRE_INVALID_SYNTAX)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_ConfigurationDownloadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownloadException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✕ Failed to download the existing Fusion configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetValidationErrors))]
    public async Task WithSourceSchema_ValidationHasErrors_ReturnsError(
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_ValidationThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);
        ;

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to validate configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription(
            CreateValidationInProgressEvent(),
            CreateValidationFailedEventWithErrors());
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--force",
            "--source-schema",
            SourceSchema);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── ! Force push is enabled.
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       ├── Client 'test-client' (ID: client-1)
            │       │   └── Operation 'abc123'
            │       ├── OpenAPI collection 'petstore' (ID: collection-1)
            │       │   └── Endpoint 'GET /pets'
            │       │       └── Invalid schema. (10:5)
            │       ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │       │   └── Tool 'test-tool'
            │       │       └── Invalid MCP schema. (5:3)
            │       └── An unexpected error occurred.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchema_WaitForApproval_NoBreakingChanges_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(waitForApproval: true, sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema",
            SourceSchema);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchema_WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(waitForApproval: true, sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreateProcessingTaskApprovedEvent(),
            CreatePublishingSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema",
            SourceSchema);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            │   ├── Approved. Processing...
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertComposedFusionSchema(schema);
    }

    [Fact]
    public async Task WithSourceSchema_WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(waitForApproval: true, sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupReleaseDeploymentSlotMutation();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateWaitForApprovalEventWithErrors(),
            CreatePublishingFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--wait-for-approval",
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       └── OpenAPI collection 'petstore' (ID: collection-1)
            │           └── Endpoint 'GET /pets'
            │               └── Invalid schema. (10:5)
            │   ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadErrors))]
    public async Task WithSourceSchema_UploadHasErrors_ReturnsError(
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation(error);
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_UploadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutationException();
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_PublishingFailedWithErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(CreatePublishingFailedEventWithErrors());
        SetupReleaseDeploymentSlotMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
            │       ├── Field 'Query.foo' has no type. SCHEMA_ERROR
            │       └── An error occurred.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchema_ReleaseDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Encountered an unexpected exception while trying to release the deployment slot after an error during the publishing process:
            Something unexpected happened.
            This is the error that caused the publishing process to fail in the first place:
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetReleaseDeploymentSlotErrors))]
    public async Task WithSourceSchema_ReleaseDeploymentSlotHasErrors_ReturnsError(
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation(sourceSchemaVersions: SourceSchemaVersions);
        SetupRequestDeploymentSlotSubscription();
        // Note: We throw during the publishing process to trigger the finally block where the slot is released.
        SetupClaimDeploymentSlotMutationException();
        SetupReleaseDeploymentSlotMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--tag",
            Tag,
            "--source-schema",
            SourceSchema);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             Encountered the following errors while trying to release the deployment slot after an error during the publishing process:
             {expectedErrorMessage}
             This is the error that caused the publishing process to fail in the first place:
             There was an unexpected error: Something unexpected happened.
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Downloading 1 source schema(s)
            │   └── ✓ Downloaded 1 source schema(s).
            ├── Requesting deployment slot
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✕ Failed to claim the deployment slot.
            └── ✕ Failed to publish Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    private void AssertFusionSchema(string schema)
    {
        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: REVIEWS) {
              cachedField: String
                @cacheControl(maxAge: 60, scope: PUBLIC)
                @fusion__field(schema: REVIEWS)
              tag1Field: String
                @fusion__field(schema: REVIEWS)
              tag2Field: String
                @fusion__field(schema: REVIEWS)
            }

            enum CacheControlScope
              @fusion__type(schema: REVIEWS) {
              "The value to cache is specific to a single user."
              PRIVATE
                @fusion__enumValue(schema: REVIEWS)
              "The value to cache is not tied to a single user."
              PUBLIC
                @fusion__enumValue(schema: REVIEWS)
            }

            "The fusion__Schema enum is a generated type used within an execution schema document to refer to a source schema in a type-safe manner."
            enum fusion__Schema {
              REVIEWS
                @fusion__schema_metadata(name: "reviews")
            }

            "The fusion__FieldDefinition scalar is used to represent a GraphQL field definition specified in the GraphQL spec."
            scalar fusion__FieldDefinition

            "The fusion__FieldSelectionMap scalar is used to represent the FieldSelectionMap type specified in the GraphQL Composite Schemas Spec."
            scalar fusion__FieldSelectionMap

            "The fusion__FieldSelectionPath scalar is used to represent a path of field names relative to the Query type."
            scalar fusion__FieldSelectionPath

            "The fusion__FieldSelectionSet scalar is used to represent a GraphQL selection set. To simplify the syntax, the outermost selection set is not wrapped in curly braces."
            scalar fusion__FieldSelectionSet

            directive @cacheControl(inheritMaxAge: Boolean maxAge: Int scope: CacheControlScope sharedMaxAge: Int vary: [String]) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION

            "The @fusion__cost directive specifies cost metadata for each source schema."
            directive @fusion__cost("The name of the source schema that defined the cost metadata." schema: fusion__Schema! "The weight defined in the source schema." weight: String!) repeatable on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            "The @fusion__enumValue directive specifies which source schema provides an enum value."
            directive @fusion__enumValue("The name of the source schema that provides the specified enum value." schema: fusion__Schema!) repeatable on ENUM_VALUE

            "The @fusion__field directive specifies which source schema provides a field in a composite type and what execution behavior it has."
            directive @fusion__field("Indicates that this field is only partially provided and must be combined with `provides`." partial: Boolean! = false "A selection set of fields this field provides in the composite schema." provides: fusion__FieldSelectionSet "The name of the source schema that originally provided this field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on FIELD_DEFINITION

            "The @fusion__implements directive specifies on which source schema an interface is implemented by an object or interface type."
            directive @fusion__implements("The name of the interface type." interface: String! "The name of the source schema on which the annotated type implements the specified interface." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE

            "The @fusion__inaccessible directive is used to prevent specific type system members from being accessible through the client-facing composite schema, even if they are accessible in the underlying source schemas."
            directive @fusion__inaccessible on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            "The @fusion__inputField directive specifies which source schema provides an input field in a composite input type."
            directive @fusion__inputField("The name of the source schema that originally provided this input field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION

            "The @fusion__listSize directive specifies list size metadata for each source schema."
            directive @fusion__listSize("The assumed size of the list as defined in the source schema." assumedSize: Int "The single slicing argument requirement of the list as defined in the source schema." requireOneSlicingArgument: Boolean "The name of the source schema that defined the list size metadata." schema: fusion__Schema! "The sized fields of the list as defined in the source schema." sizedFields: [String!] "The slicing argument default value of the list as defined in the source schema." slicingArgumentDefaultValue: Int "The slicing arguments of the list as defined in the source schema." slicingArguments: [String!]) repeatable on FIELD_DEFINITION

            "The @fusion__lookup directive specifies how the distributed executor can resolve data for an entity type from a source schema by a stable key."
            directive @fusion__lookup("The GraphQL field definition in the source schema that can be used to look up the entity." field: fusion__FieldDefinition! "Is the lookup meant as an entry point or just to provide more data." internal: Boolean! = false "A selection set on the annotated entity type that describes the stable key for the lookup." key: fusion__FieldSelectionSet! "The map describes how the key values are resolved from the annotated entity type." map: [fusion__FieldSelectionMap!]! "The path to the lookup field relative to the Query type." path: fusion__FieldSelectionPath "The name of the source schema where the annotated entity type can be looked up from." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE | UNION

            "The @fusion__requires directive specifies if a field has requirements on a source schema."
            directive @fusion__requires("The GraphQL field definition in the source schema that this field depends on." field: fusion__FieldDefinition! "The map describes how the argument values for the source schema are resolved from the arguments of the field exposed in the client-facing composite schema and from required data relative to the current type." map: [fusion__FieldSelectionMap]! "A selection set on the annotated field that describes its requirements." requirements: fusion__FieldSelectionSet! "The name of the source schema where this field has requirements to data on other source schemas." schema: fusion__Schema!) repeatable on FIELD_DEFINITION

            "The @fusion__schema_metadata directive is used to provide additional metadata for a source schema."
            directive @fusion__schema_metadata("The name of the source schema." name: String!) on ENUM_VALUE

            "The @fusion__type directive specifies which source schemas provide parts of a composite type."
            directive @fusion__type("The name of the source schema that originally provided part of the annotated type." schema: fusion__Schema!) repeatable on SCALAR | OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT

            "The @fusion__unionMember directive specifies which source schema provides a member type of a union."
            directive @fusion__unionMember("The name of the member type." member: String! "The name of the source schema that provides the specified member type." schema: fusion__Schema!) repeatable on UNION

            """);
    }

    private void AssertComposedFusionSchema(string schema)
    {
        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: PRODUCTS)
              @fusion__type(schema: REVIEWS) {
              cachedField: String
                @cacheControl(maxAge: 60, scope: PUBLIC)
                @fusion__field(schema: REVIEWS)
              field: String!
                @fusion__field(schema: PRODUCTS)
              tag1Field: String
                @fusion__field(schema: REVIEWS)
              tag2Field: String
                @fusion__field(schema: REVIEWS)
            }

            "The fusion__Schema enum is a generated type used within an execution schema document to refer to a source schema in a type-safe manner."
            enum fusion__Schema {
              PRODUCTS
                @fusion__schema_metadata(name: "products")
              REVIEWS
                @fusion__schema_metadata(name: "reviews")
            }

            "The fusion__FieldDefinition scalar is used to represent a GraphQL field definition specified in the GraphQL spec."
            scalar fusion__FieldDefinition

            "The fusion__FieldSelectionMap scalar is used to represent the FieldSelectionMap type specified in the GraphQL Composite Schemas Spec."
            scalar fusion__FieldSelectionMap

            "The fusion__FieldSelectionPath scalar is used to represent a path of field names relative to the Query type."
            scalar fusion__FieldSelectionPath

            "The fusion__FieldSelectionSet scalar is used to represent a GraphQL selection set. To simplify the syntax, the outermost selection set is not wrapped in curly braces."
            scalar fusion__FieldSelectionSet

            directive @cacheControl(inheritMaxAge: Boolean maxAge: Int scope: CacheControlScope sharedMaxAge: Int vary: [String]) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION

            "The @fusion__cost directive specifies cost metadata for each source schema."
            directive @fusion__cost("The name of the source schema that defined the cost metadata." schema: fusion__Schema! "The weight defined in the source schema." weight: String!) repeatable on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            "The @fusion__enumValue directive specifies which source schema provides an enum value."
            directive @fusion__enumValue("The name of the source schema that provides the specified enum value." schema: fusion__Schema!) repeatable on ENUM_VALUE

            "The @fusion__field directive specifies which source schema provides a field in a composite type and what execution behavior it has."
            directive @fusion__field("Indicates that this field is only partially provided and must be combined with `provides`." partial: Boolean! = false "A selection set of fields this field provides in the composite schema." provides: fusion__FieldSelectionSet "The name of the source schema that originally provided this field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on FIELD_DEFINITION

            "The @fusion__implements directive specifies on which source schema an interface is implemented by an object or interface type."
            directive @fusion__implements("The name of the interface type." interface: String! "The name of the source schema on which the annotated type implements the specified interface." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE

            "The @fusion__inaccessible directive is used to prevent specific type system members from being accessible through the client-facing composite schema, even if they are accessible in the underlying source schemas."
            directive @fusion__inaccessible on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            "The @fusion__inputField directive specifies which source schema provides an input field in a composite input type."
            directive @fusion__inputField("The name of the source schema that originally provided this input field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION

            "The @fusion__listSize directive specifies list size metadata for each source schema."
            directive @fusion__listSize("The assumed size of the list as defined in the source schema." assumedSize: Int "The single slicing argument requirement of the list as defined in the source schema." requireOneSlicingArgument: Boolean "The name of the source schema that defined the list size metadata." schema: fusion__Schema! "The sized fields of the list as defined in the source schema." sizedFields: [String!] "The slicing argument default value of the list as defined in the source schema." slicingArgumentDefaultValue: Int "The slicing arguments of the list as defined in the source schema." slicingArguments: [String!]) repeatable on FIELD_DEFINITION

            "The @fusion__lookup directive specifies how the distributed executor can resolve data for an entity type from a source schema by a stable key."
            directive @fusion__lookup("The GraphQL field definition in the source schema that can be used to look up the entity." field: fusion__FieldDefinition! "Is the lookup meant as an entry point or just to provide more data." internal: Boolean! = false "A selection set on the annotated entity type that describes the stable key for the lookup." key: fusion__FieldSelectionSet! "The map describes how the key values are resolved from the annotated entity type." map: [fusion__FieldSelectionMap!]! "The path to the lookup field relative to the Query type." path: fusion__FieldSelectionPath "The name of the source schema where the annotated entity type can be looked up from." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE | UNION

            "The @fusion__requires directive specifies if a field has requirements on a source schema."
            directive @fusion__requires("The GraphQL field definition in the source schema that this field depends on." field: fusion__FieldDefinition! "The map describes how the argument values for the source schema are resolved from the arguments of the field exposed in the client-facing composite schema and from required data relative to the current type." map: [fusion__FieldSelectionMap]! "A selection set on the annotated field that describes its requirements." requirements: fusion__FieldSelectionSet! "The name of the source schema where this field has requirements to data on other source schemas." schema: fusion__Schema!) repeatable on FIELD_DEFINITION

            "The @fusion__schema_metadata directive is used to provide additional metadata for a source schema."
            directive @fusion__schema_metadata("The name of the source schema." name: String!) on ENUM_VALUE

            "The @fusion__type directive specifies which source schemas provide parts of a composite type."
            directive @fusion__type("The name of the source schema that originally provided part of the annotated type." schema: fusion__Schema!) repeatable on SCALAR | OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT

            "The @fusion__unionMember directive specifies which source schema provides a member type of a union."
            directive @fusion__unionMember("The name of the member type." member: String! "The name of the source schema that provides the specified member type." schema: fusion__Schema!) repeatable on UNION

            """);
    }

    #region Theory Data

    public static TheoryData<
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors,
        string> GetRequestDeploymentSlotErrors() => new()
    {
        { CreateRequestDeploymentSlotUnauthorizedError(), "Unauthorized." },
        { CreateRequestDeploymentSlotApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreateRequestDeploymentSlotStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreateRequestDeploymentSlotSubgraphInvalidError(), "Subgraph is invalid." },
        { CreateRequestDeploymentSlotInvalidStateTransitionError(), "Invalid processing state transition." },
        { CreateRequestDeploymentSlotInvalidSourceMetadataError(), "Invalid source metadata input." }
    };

    public static TheoryData<
        IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors,
        string> GetClaimDeploymentSlotErrors() => new()
    {
        { CreateClaimDeploymentSlotUnauthorizedError(), "Unauthorized." },
        { CreateClaimDeploymentSlotRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateClaimDeploymentSlotInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    public static TheoryData<
        IValidateFusionConfigurationPublish_ValidateFusionConfigurationComposition_Errors,
        string> GetValidationErrors() => new()
    {
        { CreateValidationUnauthorizedError(), "Unauthorized." },
        { CreateValidationRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateValidationInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    public static TheoryData<
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors,
        string> GetUploadErrors() => new()
    {
        { CreateUploadUnauthorizedError(), "Unauthorized." },
        { CreateUploadRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateUploadInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    public static TheoryData<
        ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors,
        string> GetReleaseDeploymentSlotErrors() => new()
    {
        { CreateReleaseDeploymentSlotUnauthorizedError(), "Unauthorized." },
        { CreateReleaseDeploymentSlotRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateReleaseDeploymentSlotInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    #endregion
}
