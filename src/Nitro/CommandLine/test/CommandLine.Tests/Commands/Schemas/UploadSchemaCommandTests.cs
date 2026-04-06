using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class UploadSchemaCommandTests(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "upload",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new schema version.

            Usage:
              nitro schema upload [options]

            Options:
              --api-id <api-id> (REQUIRED)            The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)                  The tag of the schema version to deploy [env: NITRO_TAG]
              --schema-file <schema-file> (REQUIRED)  The path to the graphql file with the schema definition [env: NITRO_SCHEMA_FILE]
              --cloud-url <cloud-url>                 The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                     The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                         The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                          Show help and usage information

            Example:
              nitro schema upload \
                --api-id "<api-id>" \
                --tag "v1" \
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
            "upload",
            "--tag",
            Tag,
            "--schema-file",
            SchemaFile,
            "--api-id",
            ApiId);

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
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--schema-file",
            "nonexistent.graphql");

        // assert
        result.AssertError(
            """
            Schema file '/some/working/directory/nonexistent.graphql' does not exist.
            """);
    }

    [Fact]
    public async Task UploadSchemaThrows_ReturnsError()
    {
        // arrange
        SetupSchemaFile();
        SetupUploadSchemaMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "upload",
            "--tag",
            Tag,
            "--schema-file",
            SchemaFile,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new schema version 'v1' to API 'api-1'
            └── ✕ Failed to upload a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadSchemaErrors))]
    public async Task UploadSchemaHasErrors_ReturnsError(
        IUploadSchema_UploadSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSchemaFile();
        SetupUploadSchemaMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "upload",
            "--tag",
            Tag,
            "--schema-file",
            SchemaFile,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new schema version 'v1' to API 'api-1'
            └── ✕ Failed to upload a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadSchemaReturnsNullSchemaVersion_ReturnsError()
    {
        // arrange
        SetupSchemaFile();

        var payload = new Mock<IUploadSchema_UploadSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadSchema_UploadSchema_Errors>?)null);
        payload.SetupGet(x => x.SchemaVersion)
            .Returns((IUploadSchema_UploadSchema_SchemaVersion?)null);

        SchemasClientMock
            .Setup(x => x.UploadSchemaAsync(
                ApiId, Tag, It.IsAny<Stream>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload.Object);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "upload",
            "--tag",
            Tag,
            "--schema-file",
            SchemaFile,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload schema.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new schema version 'v1' to API 'api-1'
            └── ✕ Failed to upload a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadsSchema_ReturnsSuccess()
    {
        // arrange
        SetupSchemaFile();
        SetupUploadSchemaMutation();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "upload",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--schema-file",
            SchemaFile);

        // assert
        result.AssertSuccess(
            """
            Uploading new schema version 'v1' to API 'api-1'
            └── ✓ Uploaded new schema version 'v1'.
            """);
    }

    #region Error Theory Data

    public static TheoryData<
        IUploadSchema_UploadSchema_Errors,
        string> GetUploadSchemaErrors() => new()
    {
        { CreateUploadSchemaUnauthorizedError(), "Unauthorized." },
        { CreateUploadSchemaApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreateUploadSchemaDuplicatedTagError(), $"Tag '{Tag}' already exists." },
        { CreateUploadSchemaConcurrentOperationError(), "A concurrent operation is in progress." }
    };

    #endregion
}
