using ChilliCream.Nitro.Client.Schemas;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class DownloadSchemaCommandTests(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    private const string OutputFile = "/tmp/schema.graphql";

    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download a schema from a stage.

            Usage:
              nitro schema download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --file <file> (REQUIRED)      The file where the schema is stored
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro schema download \
                --api-id "<api-id>" \
                --stage "dev" \
                --file ./schema.graphqls
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
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--file",
            OutputFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--file' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task DownloadSchemaThrows_ReturnsError()
    {
        // arrange
        SetupDownloadSchemaException();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--file",
            OutputFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Downloading schema from stage 'dev' of API 'api-1'
            └── ✕ Failed to download the schema.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task SchemaNotFound_ReturnsError()
    {
        // arrange
        SetupMissingDownloadSchema();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--file",
            OutputFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Downloading schema from stage 'dev' of API 'api-1'
            └── ✕ Failed to download the schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find a published schema on stage 'dev'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Success_DownloadsSchema()
    {
        // arrange
        SetupDownloadSchema();
        var tempFile = SetupCreateFile(OutputFile);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--file",
            OutputFile);

        // assert
        result.AssertSuccess(
            """
            Downloading schema from stage 'dev' of API 'api-1'
            └── ✓ Downloaded the schema from stage 'dev'.
            """);
        Assert.Equal("type Query { hello: String }", await File.ReadAllTextAsync(tempFile));
    }

    [Fact]
    public async Task Success_DeletesExistingFile_BeforeDownload()
    {
        // arrange
        SetupDownloadSchema();
        SetupFile(OutputFile, "old content");
        var tempFile = SetupCreateFile(OutputFile);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "download",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--file",
            OutputFile);

        // assert
        result.AssertSuccess(
            """
            Downloading schema from stage 'dev' of API 'api-1'
            └── ✓ Downloaded the schema from stage 'dev'.
            """);
        Assert.Equal("type Query { hello: String }", await File.ReadAllTextAsync(tempFile));
    }
}
