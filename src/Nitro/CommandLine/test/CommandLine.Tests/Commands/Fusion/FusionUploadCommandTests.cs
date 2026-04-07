using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionUploadCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "fusion",
            "upload",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a source schema for a later composition.

            Usage:
              nitro fusion upload [options]

            Options:
              --api-id <api-id> (REQUIRED)                              The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)                                    The tag of the schema version to deploy [env: NITRO_TAG]
              -f, --source-schema-file <source-schema-file> (REQUIRED)  The path to a source schema file (.graphqls) or directory containing a source schema file
              -w, --working-directory <working-directory>               Set the working directory for the command
              --cloud-url <cloud-url>                                   The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                       The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                           The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                            Show help and usage information

            Example:
              nitro fusion upload \
                --api-id "<api-id>" \
                --tag "v1" \
                --source-schema-file ./products/schema.graphqls
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
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertError(
            """
            Schema file '/some/working/directory/products/schema.graphqls' does not exist.
            """);
    }

    [Theory]
    [MemberData(nameof(GetUploadSourceSchemaErrors))]
    public async Task UploadSourceSchemaMutationHasErrors_ReturnsError(
        IUploadFusionSubgraph_UploadFusionSubgraph_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupUploadSourceSchemaMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new source schema version 'v1' to API 'api-1'
            └── ✕ Failed to upload a new source schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadSourceSchema_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        var capturedStream = SetupUploadSourceSchemaMutation();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        await AssertFusionSourceSchemaArchive(capturedStream);
        result.AssertSuccess(
            """
            Uploading new source schema version 'v1' to API 'api-1'
            └── ✓ Uploaded new source schema version 'v1'.
            """);
    }

    [Fact]
    public async Task UploadSourceSchemaThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupUploadSourceSchemaMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new source schema version 'v1' to API 'api-1'
            └── ✕ Failed to upload a new source schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #region Theory Data

    public static TheoryData<
        IUploadFusionSubgraph_UploadFusionSubgraph_Errors,
        string> GetUploadSourceSchemaErrors() => new()
    {
        { CreateUploadSourceSchemaUnauthorizedError(), "Not authorized to upload." },
        { CreateUploadSourceSchemaDuplicatedTagError("Tag 'v1' already exists."), "Tag 'v1' already exists." },
        { CreateUploadSourceSchemaConcurrentOperationError(), "A concurrent operation is in progress." },
        {
            CreateUploadSourceSchemaInvalidArchiveError(),
            "The server received an invalid archive. This indicates a bug in the tooling. Please notify ChilliCream. Error received: The archive is invalid."
        }
    };

    #endregion
}
