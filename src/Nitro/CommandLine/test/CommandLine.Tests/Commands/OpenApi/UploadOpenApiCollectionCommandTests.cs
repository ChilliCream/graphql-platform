using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class UploadOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new OpenAPI collection version.

            Usage:
              nitro openapi upload [options]

            Options:
              --openapi-collection-id <openapi-collection-id> (REQUIRED)  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                      The tag of the schema version to deploy [env: NITRO_TAG]
              -p, --pattern <pattern> (REQUIRED)                          One or more glob patterns for selecting OpenAPI document files
              --cloud-url <cloud-url>                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                              Show help and usage information

            Example:
              nitro openapi upload \
                --openapi-collection-id "<collection-id>" \
                --tag "v1" \
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
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "openapi",
            "upload");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--openapi-collection-id' is required.
            Option '--tag' is required.
            Option '--pattern' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoFilesFound_ReturnsError()
    {
        // arrange
        SetupEmptyGlobMatch();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.AssertError(
            """
            Could not find any OpenAPI documents with the provided pattern.
            """);
    }

    [Fact]
    public async Task UploadOpenApiCollectionThrows_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupUploadOpenApiCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version 'v1' for OpenAPI collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadOpenApiCollectionErrors))]
    public async Task UploadOpenApiCollectionHasErrors_ReturnsError(
        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        SetupOpenApiDocument();
        SetupUploadOpenApiCollectionMutation(mutationError);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version 'v1' for OpenAPI collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadOpenApiCollectionReturnsNullVersion_ReturnsError()
    {
        // arrange
        SetupOpenApiDocument();
        SetupUploadOpenApiCollectionMutationNullVersion();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new version 'v1' for OpenAPI collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload OpenAPI collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadsCollection_ReturnsSuccess()
    {
        // arrange
        SetupOpenApiDocument();
        var capturedStream = SetupUploadOpenApiCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        await AssertOpenApiCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Uploading new version 'v1' for OpenAPI collection 'oa-1'
            └── ✓ Uploaded new OpenAPI collection version 'v1'.
            """);
    }

    [Fact]
    public async Task InvalidDocument_ReturnsError()
    {
        // arrange
        SetupInvalidOpenApiDocument();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "upload",
            "--tag",
            Tag,
            "--openapi-collection-id",
            OpenApiCollectionId,
            "--pattern",
            "*.graphql");

        // assert
        result.AssertError(
            """
            Encountered an error while trying to parse '/some/working/directory/document.graphql': Operation must be annotated with @http directive.
            """);
    }

    public static TheoryData<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors, string>
        GetUploadOpenApiCollectionErrors()
    {
        var data = new TheoryData<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors, string>
        {
            {
                new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                    "oa-1", "OpenAPI collection not found."),
                """
                OpenAPI collection not found.
                """
            },
            {
                new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized to upload."),
                """
                Not authorized to upload.
                """
            },
            {
                new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_DuplicatedTagError(
                    "DuplicatedTagError", "Tag 'v1' already exists."),
                """
                Tag 'v1' already exists.
                """
            },
            {
                new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_ConcurrentOperationError(
                    "ConcurrentOperationError", "A concurrent operation is in progress."),
                """
                A concurrent operation is in progress.
                """
            },
            {
                new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_InvalidOpenApiCollectionArchiveError(
                    "Invalid archive format."),
                """
                The server received an invalid archive. This indicates a bug in the tooling. Please notify ChilliCream. Error received: Invalid archive format.
                """
            }
        };

        var unexpectedError = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>();
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
