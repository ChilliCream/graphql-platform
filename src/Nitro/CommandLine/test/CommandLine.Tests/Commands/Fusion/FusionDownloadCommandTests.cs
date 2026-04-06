using ChilliCream.Nitro.CommandLine.Commands.Fusion;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

// TODO: Assert file has been downloaded
// TODO: Test extension and version mismatch
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
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --version <version>           The version of the archive to download [default: 2.0.0]
              --output-file <output-file>   The file path to write the output to [env: NITRO_OUTPUT_FILE]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro fusion download \
                --api-id "<api-id>" \
                --stage "dev" \
                --output-file ./gateway.far
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
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

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
            "download");

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

    // TODO: createfile needs to be properly setup
    [Fact]
    public async Task DownloadFarFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload();

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

            """);
    }

    [Fact]
    public async Task DownloadFgpFile_ReturnsSuccess()
    {
        // arrange
        SetupFusionConfigurationDownload("1.0.0", ArchiveFormats.Fgp);

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
            The API with the given ID does not exist or does not have a download URL.
            """);
    }
}
