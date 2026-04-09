using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class CreateMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new MCP feature collection.

            Usage:
              nitro mcp create [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --name <name>            The name of the MCP Feature Collection [env: NITRO_MCP_FEATURE_COLLECTION_NAME]
                            --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information

            Example:
              nitro mcp create \
                --name "my-collection" \
                --api-id "<api-id>"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupNoAuthentication();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredName_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            Missing required option '--name'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupSession();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--name",
            McpFeatureCollectionName);

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task CreateMcpFeatureCollectionThrows_ReturnsError()
    {
        // arrange
        SetupCreateMcpFeatureCollectionMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating MCP feature collection 'my-mcp' for API 'api-1'
            └── ✕ Failed to create the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetCreateMcpFeatureCollectionErrors))]
    public async Task CreateMcpFeatureCollectionHasErrors_ReturnsError(
        ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupCreateMcpFeatureCollectionMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Creating MCP feature collection 'my-mcp' for API 'api-1'
            └── ✕ Failed to create the MCP feature collection.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task CreateMcpFeatureCollectionReturnsNullResult_ReturnsError()
    {
        // arrange
        SetupCreateMcpFeatureCollectionMutationNullResult();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating MCP feature collection 'my-mcp' for API 'api-1'
            └── ✕ Failed to create the MCP feature collection.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        SetupCreateMcpFeatureCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.AssertSuccess(
            """
            Creating MCP feature collection 'my-mcp' for API 'api-1'
            └── ✓ Created MCP feature collection 'my-mcp'.

            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        SetupInteractionMode(InteractionMode.JsonOutput);
        SetupCreateMcpFeatureCollectionMutation();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "create",
            "--api-id",
            ApiId,
            "--name",
            McpFeatureCollectionName);

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mcp-1",
              "name": "my-mcp"
            }
            """);
    }

    public static TheoryData<ICreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors, string>
        GetCreateMcpFeatureCollectionErrors() => new()
    {
        {
            new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors_ApiNotFoundError(
                "API not found", "ApiNotFoundError", "api-1"),
            "API not found"
        },
        {
            new CreateMcpFeatureCollectionCommandMutation_CreateMcpFeatureCollection_Errors_UnauthorizedOperation(
                "Not authorized", "UnauthorizedOperation"),
            "Not authorized"
        }
    };
}
