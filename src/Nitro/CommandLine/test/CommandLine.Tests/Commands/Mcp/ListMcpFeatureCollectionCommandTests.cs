namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp;

public sealed class ListMcpFeatureCollectionCommandTests(NitroCommandFixture fixture) : McpCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all MCP feature collections of an API.

            Usage:
              nitro mcp list [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro mcp list --api-id "<api-id>"
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
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);

        var result = await ExecuteCommandAsync(
            "mcp",
            "list");

        // assert
        result.AssertError(
            """
            Could not determine workspace. Either login via `nitro login` or specify the '--workspace-id' option.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "mcp",
            "list");

        // assert
        result.AssertError(
            """
            Missing required option '--api-id'.
            """);
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListMcpFeatureCollectionsQuery(
            items: [("mcp-1", "auth-tools"), ("mcp-2", "data-tools")]);

        var command = StartInteractiveCommand(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListMcpFeatureCollectionsQuery(
            items: [("mcp-1", "auth-tools"), ("mcp-2", "data-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListMcpFeatureCollectionsQuery();

        var command = StartInteractiveCommand(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListMcpFeatureCollectionsQuery();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListMcpFeatureCollectionsQuery(
            cursor: "cursor-1",
            items: [("mcp-1", "auth-tools")]);

        var command = StartInteractiveCommand(
            "mcp",
            "list",
            "--api-id",
            ApiId,
            "--cursor",
            "cursor-1");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListMcpFeatureCollectionsQuery(
            cursor: "cursor-1",
            items: [("mcp-1", "auth-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--api-id",
            ApiId,
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursorPagination_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListMcpFeatureCollectionsQuery(
            endCursor: "cursor-2",
            hasNextPage: true,
            items: [("mcp-1", "auth-tools"), ("mcp-2", "data-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mcp-1",
                  "name": "auth-tools"
                },
                {
                  "id": "mcp-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Fact]
    public async Task ListMcpFeatureCollectionsThrows_ReturnsError()
    {
        // arrange
        SetupListMcpFeatureCollectionsQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "mcp",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
