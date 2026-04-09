using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mcp;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class UploadMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new MCP feature collection version.

            Usage:
              nitro mcp upload [options]

            Options:
              --mcp-feature-collection-id <mcp-feature-collection-id> (REQUIRED)  The ID of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_ID]
              --tag <tag> (REQUIRED)                                              The tag of the schema version to deploy [env: NITRO_TAG]
              -p, --prompt-pattern <prompt-pattern>                               One or more file patterns to locate MCP prompt definition files (*.json)
              -t, --tool-pattern <tool-pattern>                                   One or more file patterns to locate MCP tool definition files (*.graphql)
              --cloud-url <cloud-url>                                             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                                      Show help and usage information

            Example:
              nitro mcp upload \
                --mcp-feature-collection-id "<collection-id>" \
                --tag "v1" \
                --prompt-pattern "./prompts/**/*.json" \
                --tool-pattern "./tools/**/*.graphql"
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
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task MissingRequiredOptions_ReturnsError()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--mcp-feature-collection-id' is required.
            Option '--tag' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoFilesFound_ReturnsError()
    {
        // arrange
        SetupEmptyMcpDefinitionFiles();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.AssertError(
            """
            Could not find any MCP prompt or tool definition files with the provided patterns.
            """);
    }

    [Fact]
    public async Task UploadMcpFeatureCollectionThrows_ReturnsError()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupUploadMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadMcpFeatureCollectionErrors))]
    public async Task UploadMcpFeatureCollectionHasErrors_ReturnsError(
        IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupUploadMcpFeatureCollectionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadMcpFeatureCollectionReturnsNullVersion_ReturnsError()
    {
        // arrange
        SetupMcpDefinitionFiles();
        SetupUploadMcpFeatureCollectionMutationNullVersion();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload MCP Feature Collection version.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✕ Failed to upload a new MCP feature collection version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task UploadsMcpFeatureCollection_ReturnsSuccess()
    {
        // arrange
        SetupMcpDefinitionFiles();
        var capturedStream = SetupUploadMcpFeatureCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "upload",
            "--tag",
            Tag,
            "--mcp-feature-collection-id",
            McpFeatureCollectionId,
            "--prompt-pattern",
            "**/*.json",
            "--tool-pattern",
            "**/*.graphql");

        // assert
        await AssertMcpFeatureCollectionArchive(capturedStream);
        result.AssertSuccess(
            """
            Uploading new MCP feature collection version 'v1' for collection 'mcp-1'
            ├── Found 1 prompt(s) and 1 tool(s).
            └── ✓ Uploaded new MCP feature collection version 'v1'.
            """);
    }

    public static TheoryData<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors, string>
        GetUploadMcpFeatureCollectionErrors()
    {
        var unexpectedError = new Mock<IUploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        return new()
        {
            {
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_McpFeatureCollectionNotFoundError(
                    "mcp-1", "MCP Feature Collection not found."),
                "MCP Feature Collection not found."
            },
            {
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_UnauthorizedOperation(
                    "UnauthorizedOperation", "Not authorized to upload."),
                "Not authorized to upload."
            },
            {
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_DuplicatedTagError(
                    "DuplicatedTagError", "Tag 'v1' already exists."),
                "Tag 'v1' already exists."
            },
            {
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_ConcurrentOperationError(
                    "ConcurrentOperationError", "A concurrent operation is in progress."),
                "A concurrent operation is in progress."
            },
            {
                new UploadMcpFeatureCollectionCommandMutation_UploadMcpFeatureCollection_Errors_InvalidMcpFeatureCollectionArchiveError(
                    "Invalid archive format."),
                "The server received an invalid archive. This indicates a bug in the tooling. Please notify ChilliCream. Error received: Invalid archive format."
            },
            {
                unexpectedError.Object,
                "Unexpected mutation error: Something went wrong."
            }
        };
    }
}
