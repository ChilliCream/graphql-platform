using ChilliCream.Nitro.Client;

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
    public async Task NoOptions_ReturnsError(InteractionMode mode)
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
    public async Task NoArchiveOrSourceSchemaFileOrSourceSchema_ReturnsError(InteractionMode mode)
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
    public async Task MultipleExclusiveOptions_ReturnsError(InteractionMode mode)
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
        SetupFusionConfigurationUploadMutation();
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
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
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync("fusion", "publish");

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ! There is no existing configuration on 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
    }

    [Theory]
    [MemberData(nameof(GetRequestDeploymentSlotErrors))]
    public async Task WithArchive_RequestDeploymentSlotHasErrors_ReturnsError(
        IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation(error);

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

    [Fact]
    public async Task WithArchive_ClaimDeploymentSlotThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutationException();

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
            │   ├── Request ID: request-id
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
            TODO
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-id
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✕ Failed to validate the new configuration.
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
            Failed to commit Fusion archive.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration to stage 'dev' of API 'api-1'
            ├── Requesting deployment slot
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✕ Failed to upload the new configuration.
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
            Source schema file '/some/working/directory/products/schema.graphqls' does not exist.
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
        SetupFusionConfigurationUploadMutation();
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ! There is no existing configuration on 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
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
        SetupFusionConfigurationUploadMutation();
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ! There is no existing configuration on 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
    }

    #endregion

    #region Source Schema

    [Fact]
    public async Task WithSourceSchema_SourceSchemaDoesNotExist_ReturnsError()
    {
        // arrange
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
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ! There is no existing configuration on 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
    }

    [Fact]
    public async Task WithSourceSchema_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);

        SetupSourceSchemaDownload();
        SetupRequestDeploymentSlotMutation();
        SetupRequestDeploymentSlotSubscription();
        SetupClaimDeploymentSlotMutation();
        SetupFusionConfigurationDownload();
        SetupFusionConfigurationValidationMutation();
        SetupFusionConfigurationValidationSubscription();
        SetupFusionConfigurationUploadMutation();
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
            │   ├── Request ID: request-id
            │   └── ✓ Deployment slot ready.
            ├── Claiming deployment slot
            │   └── ✓ Claimed deployment slot.
            ├── Downloading existing configuration from 'dev'
            │   └── ! There is no existing configuration on 'dev'.
            ├── Composing new configuration
            │   └── ✓ Composed new configuration.
            ├── Validating configuration against 'dev'
            │   └── ✓ Validated the Fusion configuration.
            ├── Uploading configuration to 'dev'
            │   └── ✓ Uploaded configuration.
            └── ✓ Published configuration 'v1' to 'dev'.
            """);
    }

    #endregion

// --------------------------- OLD

// [Fact]
// public async Task ArchiveFileDoesNotExist_ReturnsError_NonInteractive()
// {
//     // arrange
//     var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
//     fileSystem.Setup(x => x.GetCurrentDirectory())
//         .Returns("/tmp");
//     fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
//         .Returns(false);
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.AssertError(
//         """
//         Archive file 'fusion.far' does not exist.
//         """);
//
//     fileSystem.VerifyAll();
// }
//
// [Fact]
// public async Task ClientThrowsException_ReturnsError_NonInteractive()
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishExceptionSetup(
//         new NitroClientGraphQLException("Some message.", "SOME_CODE"));
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.StdOut.MatchInlineSnapshot(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   └── ✕ Failed to request a deployment slot.
//         └── ✕ Failed to publish Fusion configuration.
//         """);
//     result.StdErr.MatchInlineSnapshot(
//         """
//         The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
//         """);
//     Assert.Equal(1, result.ExitCode);
//
//     client.VerifyAll();
// }
//
// [Theory]
// [InlineData(InteractionMode.Interactive)]
// [InlineData(InteractionMode.JsonOutput)]
// public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishExceptionSetup(
//         new NitroClientGraphQLException("Some message.", "SOME_CODE"));
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(mode)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.StdErr.MatchInlineSnapshot(
//         """
//         The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
//         """);
//     Assert.Equal(1, result.ExitCode);
//
//     client.VerifyAll();
// }
//
// [Fact]
// public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishExceptionSetup(
//         new NitroClientAuthorizationException());
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.StdOut.MatchInlineSnapshot(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   └── ✕ Failed to request a deployment slot.
//         └── ✕ Failed to publish Fusion configuration.
//         """);
//     result.StdErr.MatchInlineSnapshot(
//         """
//         The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
//         """);
//     Assert.Equal(1, result.ExitCode);
//
//     client.VerifyAll();
// }
//
// [Theory]
// [InlineData(InteractionMode.Interactive)]
// [InlineData(InteractionMode.JsonOutput)]
// public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishExceptionSetup(
//         new NitroClientAuthorizationException());
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(mode)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.StdErr.MatchInlineSnapshot(
//         """
//         The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
//         """);
//     Assert.Equal(1, result.ExitCode);
//
//     client.VerifyAll();
// }
//
// [Fact]
// public async Task Publish_Should_UploadArchive_When_ArchiveFileProvided()
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishSuccessSetup(
//         CreateReadyEvent(),
//         CreateCommitSuccessEvent());
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.AssertSuccess(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   ├── Request ID: request-123
//         │   └── ✓ Deployment slot ready.
//         ├── Claiming deployment slot
//         │   └── ✓ Claimed deployment slot.
//         ├── Uploading configuration to 'production'
//         │   └── ✓ Uploaded configuration.
//         └── ✓ Published configuration to 'production'.
//         """);
//
//     client.VerifyAll();
//     fileSystem.VerifyAll();
// }
//
// [Fact]
// public async Task Publish_Should_HandleSubscriptionEvents_When_Queued()
// {
//     // arrange
//     var queuedEvent = CreateQueuedEvent(3);
//     var readyEvent = CreateReadyEvent();
//
//     var (client, fileSystem) = CreateArchivePublishSuccessSetup(
//         [queuedEvent, readyEvent],
//         CreateCommitSuccessEvent());
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.AssertSuccess(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   ├── Request ID: request-123
//         │   ├── Queued at position 3.
//         │   └── ✓ Deployment slot ready.
//         ├── Claiming deployment slot
//         │   └── ✓ Claimed deployment slot.
//         ├── Uploading configuration to 'production'
//         │   └── ✓ Uploaded configuration.
//         └── ✓ Published configuration to 'production'.
//         """);
//
//     client.VerifyAll();
//     fileSystem.VerifyAll();
// }
//
// [Fact]
// public async Task Publish_Should_HandleSubscriptionEvents_When_PublishSucceeds()
// {
//     // arrange
//     var (client, fileSystem) = CreateArchivePublishSuccessSetup(
//         CreateReadyEvent(),
//         CreateCommitSuccessEvent());
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.AssertSuccess(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   ├── Request ID: request-123
//         │   └── ✓ Deployment slot ready.
//         ├── Claiming deployment slot
//         │   └── ✓ Claimed deployment slot.
//         ├── Uploading configuration to 'production'
//         │   └── ✓ Uploaded configuration.
//         └── ✓ Published configuration to 'production'.
//         """);
//
//     client.VerifyAll();
//     fileSystem.VerifyAll();
// }
//
// [Fact]
// public async Task Publish_Should_HandleSubscriptionEvents_When_PublishFails()
// {
//     // arrange
//     var errorMock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors>(MockBehavior.Strict);
//     errorMock.SetupGet(x => x.Message).Returns("Composition failed.");
//
//     var failedEvent = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingFailed>(MockBehavior.Strict);
//     failedEvent.As<IFusionConfigurationPublishingFailed>()
//         .SetupGet(x => x.Errors)
//         .Returns(new[] { errorMock.Object });
//
//     var (client, fileSystem) = CreateArchivePublishWithCommitEvents(
//         CreateReadyEvent(),
//         failedEvent.Object);
//
//     var releasePayload = new Mock<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition>(MockBehavior.Strict);
//     releasePayload.SetupGet(x => x.Errors)
//         .Returns((IReadOnlyList<ICancelFusionConfigurationPublish_CancelFusionConfigurationComposition_Errors>?)null);
//     client.Setup(x => x.ReleaseDeploymentSlotAsync(
//             DefaultRequestId,
//             It.IsAny<CancellationToken>()))
//         .ReturnsAsync(releasePayload.Object);
//
//     // act
//     var result = await new CommandBuilder(fixture)
//         .AddService(client.Object)
//         .AddService(fileSystem.Object)
//         .AddApiKey()
//         .AddInteractionMode(InteractionMode.NonInteractive)
//         .AddArguments(
//             "fusion",
//             "publish",
//             "--api-id",
//             DefaultApiId,
//             "--stage",
//             DefaultStage,
//             "--tag",
//             DefaultTag,
//             "--archive",
//             DefaultArchiveFile)
//         .ExecuteAsync();
//
//     // assert
//     result.StdOut.MatchInlineSnapshot(
//         """
//         Publishing Fusion configuration to stage 'production' of API 'api-1'
//         ├── Requesting deployment slot
//         │   ├── Request ID: request-123
//         │   └── ✓ Deployment slot ready.
//         ├── Claiming deployment slot
//         │   └── ✓ Claimed deployment slot.
//         ├── Uploading configuration to 'production'
//         │   └── ✕ Failed to upload the new configuration.
//         └── ✕ Failed to publish Fusion configuration.
//         """);
//     result.StdErr.MatchInlineSnapshot(
//         """
//         Composition failed.
//         The commit has failed.
//         The commit has failed.
//         """);
//     Assert.Equal(1, result.ExitCode);
//
//     client.VerifyAll();
//     fileSystem.VerifyAll();
// }
//
// // --- Helpers ---
//
// private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
//     CreateReadyEvent()
// {
//     return Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsReady>();
// }
//
// private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
//     CreateCommitSuccessEvent()
// {
//     return Mock.Of<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_FusionConfigurationPublishingSuccess>();
// }
//
// private static IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged
//     CreateQueuedEvent(int position)
// {
//     var mock = new Mock<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_ProcessingTaskIsQueued>(MockBehavior.Strict);
//     mock.As<IProcessingTaskIsQueued>()
//         .SetupGet(x => x.QueuePosition)
//         .Returns(position);
//     return mock.Object;
// }
//
// private static Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>
//     CreateSuccessPayload()
// {
//     var payload = new Mock<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish>(MockBehavior.Strict);
//     payload.SetupGet(x => x.Errors)
//         .Returns((IReadOnlyList<IBeginFusionConfigurationPublish_BeginFusionConfigurationPublish_Errors>?)null);
//     payload.SetupGet(x => x.RequestId).Returns(DefaultRequestId);
//     return payload;
// }
//
// private static Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>
//     CreateCommitPayload()
// {
//     var payload = new Mock<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish>(MockBehavior.Strict);
//     payload.SetupGet(x => x.Errors)
//         .Returns((IReadOnlyList<ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors>?)null);
//     return payload;
// }
//
// private static Mock<IFileSystem> CreateArchiveFileSystem()
// {
//     var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
//     fileSystem.Setup(x => x.GetCurrentDirectory())
//         .Returns("/tmp");
//     fileSystem.Setup(x => x.FileExists(DefaultArchiveFile))
//         .Returns(true);
//     fileSystem.Setup(x => x.OpenReadStream(DefaultArchiveFile))
//         .Returns(new MemoryStream("archive-content"u8.ToArray()));
//     return fileSystem;
// }
//
// private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
//     CreateArchivePublishSuccessSetup(
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged beginEvent,
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent)
// {
//     return CreateArchivePublishSuccessSetup([beginEvent], commitEvent);
// }
//
// private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
//     CreateArchivePublishSuccessSetup(
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] beginEvents,
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent)
// {
//     return CreateArchivePublishWithCommitEvents(beginEvents, commitEvent, createCommitPayload: true);
// }
//
// private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
//     CreateArchivePublishWithCommitEvents(
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged beginEvent,
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent,
//         bool createCommitPayload = true)
// {
//     return CreateArchivePublishWithCommitEvents([beginEvent], commitEvent, createCommitPayload);
// }
//
// private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
//     CreateArchivePublishWithCommitEvents(
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged[] beginEvents,
//         IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged commitEvent,
//         bool createCommitPayload = true)
// {
//     var payload = CreateSuccessPayload();
//     var claimPayload = new Mock<IStartFusionConfigurationPublish_StartFusionConfigurationComposition>(MockBehavior.Strict);
//     claimPayload.SetupGet(x => x.Errors)
//         .Returns((IReadOnlyList<IStartFusionConfigurationPublish_StartFusionConfigurationComposition_Errors>?)null);
//     var commitPayloadMock = CreateCommitPayload();
//
//     var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
//     client.Setup(x => x.RequestDeploymentSlotAsync(
//             DefaultApiId,
//             DefaultStage,
//             DefaultTag,
//             null,
//             null,
//             null,
//             false,
//             null,
//             It.IsAny<CancellationToken>()))
//         .ReturnsAsync(payload.Object);
//
//     var subscriptionCallCount = 0;
//     client.Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
//             DefaultRequestId,
//             It.IsAny<CancellationToken>()))
//         .Returns((string _, CancellationToken ct) =>
//         {
//             var call = Interlocked.Increment(ref subscriptionCallCount);
//             if (call == 1)
//             {
//                 return ToAsyncEnumerable(beginEvents, ct);
//             }
//
//             return ToAsyncEnumerable([commitEvent], ct);
//         });
//
//     client.Setup(x => x.ClaimDeploymentSlotAsync(
//             DefaultRequestId,
//             It.IsAny<CancellationToken>()))
//         .ReturnsAsync(claimPayload.Object);
//
//     client.Setup(x => x.CommitFusionArchiveAsync(
//             DefaultRequestId,
//             It.IsAny<Stream>(),
//             It.IsAny<CancellationToken>()))
//         .ReturnsAsync(commitPayloadMock.Object);
//
//     var fileSystem = CreateArchiveFileSystem();
//
//     return (client, fileSystem);
// }
//
// private static (Mock<IFusionConfigurationClient> Client, Mock<IFileSystem> FileSystem)
//     CreateArchivePublishExceptionSetup(Exception ex)
// {
//     var client = new Mock<IFusionConfigurationClient>(MockBehavior.Strict);
//     client.Setup(x => x.RequestDeploymentSlotAsync(
//             DefaultApiId,
//             DefaultStage,
//             DefaultTag,
//             null,
//             null,
//             null,
//             false,
//             null,
//             It.IsAny<CancellationToken>()))
//         .ThrowsAsync(ex);
//
//     var fileSystem = CreateArchiveFileSystem();
//
//     return (client, fileSystem);
// }
//
// private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
//     IEnumerable<T> items,
//     [EnumeratorCancellation] CancellationToken ct = default)
// {
//     foreach (var item in items)
//     {
//         yield return item;
//     }
//
//     await Task.CompletedTask;
// }

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

    #endregion
}
