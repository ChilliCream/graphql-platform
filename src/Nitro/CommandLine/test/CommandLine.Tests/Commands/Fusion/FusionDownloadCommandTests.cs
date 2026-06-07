using ChilliCream.Nitro.CommandLine.Commands.Fusion;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionDownloadCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download the most recent gateway configuration.

            Usage:
              nitro fusion download [options]

            Options:
              --api-id <api-id>            The ID of the API [env: NITRO_API_ID]
              --stage <stage>              The name of the stage [env: NITRO_STAGE]
              --version <version>          The version of the archive to download [default: 2.0.0]
              --output-file <output-file>  The file path to write the output to [env: NITRO_OUTPUT_FILE]
              --cloud-url <cloud-url>      The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>          The API key used for authentication [env: NITRO_API_KEY]
              --output <json>              The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help               Show help and usage information

            Example:
              nitro fusion download \
                --api-id "<api-id>" \
                --stage "dev" \
                --output-file ./gateway.far
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Download_Should_ReturnError_When_ApiIdNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--api-id'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task Download_Should_ReturnError_When_StageNotProvided(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--stage'.
            """);
        Assert.Equal(1, result.ExitCode);
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
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    #region Option Validation

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task InvalidVersion_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--version",
            "invalid");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--version' received an invalid value: invalid
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    [Fact]
    public async Task DownloadFarFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload();
        var outputStream = SetupCreateFile(ArchiveFile);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Downloaded Fusion configuration to '/some/working/directory/fusion.far'.
            """);
        Assert.True(outputStream.ToArray().Length > 0);
    }

    [Fact]
    public async Task DownloadFgpFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload("1.0.0", ArchiveFormats.Fgp);
        var outputStream = SetupCreateFile("gateway.fgp");

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "gateway.fgp",
            "--version",
            "1.0.0");

        // assert
        result.AssertSuccess(
            """
            Downloaded Fusion configuration to '/some/working/directory/gateway.fgp'.
            """);
        Assert.True(outputStream.ToArray().Length > 0);
    }

    [Fact]
    public async Task DownloadFgpFile_VersionIsNot1_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "gateway.fgp",
            "--version",
            "2.0.0");

        // assert
        result.AssertError(
            """
            Specify '--version 1.0.0' if you want to download a '.fgp' legacy Fusion archive.
            """);
    }

    [Fact]
    public async Task DownloadFarFile_VersionIs1_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--output-file",
            "gateway.far",
            "--version",
            "1.0.0");

        // assert
        result.AssertError(
            """
            Specify the '.fgp' extension through the '--output-file' option, if you want to download a legacy Fusion archive.
            """);
    }

    [Fact]
    public async Task FusionConfigurationNotFound_ReturnsError()
    {
        // arrange
        SetupMissingFusionConfigurationDownload();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            The API with the given ID does not exist or there is no Fusion configuration that supports version '2.0.0'.
            """);
    }

    [Fact]
    public async Task Download_Should_PromptForStage_When_ApiProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListStagesQuery(("stage-1", Stage));
        SetupFusionConfigurationDownload();
        SetupCreateFile(ArchiveFile);

        var command = StartInteractiveCommand(
            "fusion",
            "download",
            "--api-id",
            ApiId,
            "--output-file",
            ArchiveFile);

        // act
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Download_Should_PromptForApi_When_StageProvided_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectApisPrompt((ApiId, "products"));
        SetupFusionConfigurationDownload();
        SetupCreateFile(ArchiveFile);

        var command = StartInteractiveCommand(
            "fusion",
            "download",
            "--stage",
            Stage,
            "--output-file",
            ArchiveFile);

        // act
        command.SelectOption(0); // Select API
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task Download_Should_PromptForApiAndStage_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListStagesQuery(("stage-1", Stage));
        SetupFusionConfigurationDownload();
        SetupCreateFile(ArchiveFile);

        var command = StartInteractiveCommand(
            "fusion",
            "download",
            "--output-file",
            ArchiveFile);

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select stage
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }
}
