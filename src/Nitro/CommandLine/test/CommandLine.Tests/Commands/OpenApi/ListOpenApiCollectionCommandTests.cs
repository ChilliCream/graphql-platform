namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class ListOpenApiCollectionCommandTests(NitroCommandFixture fixture) : OpenApiCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "openapi",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all OpenAPI collections of an API.

            Usage:
              nitro openapi list [options]

            Options:
              --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro openapi list --api-id "<api-id>"
            """);
    }

    [Fact]
    public async Task NoApiKey_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        SetupInteractionMode(InteractionMode.Interactive);
        SetupNoAuthentication();

        var result = await ExecuteCommandAsync(
            "openapi",
            "list");

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
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
            "openapi",
            "list");

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);

        var result = await ExecuteCommandAsync(
            "openapi",
            "list");

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListOpenApiCollectionsQuery(
            items: [("openapi-1", "auth-tools"), ("openapi-2", "data-tools")]);

        var command = StartInteractiveCommand(
            "openapi",
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
        SetupListOpenApiCollectionsQuery(
            items: [("openapi-1", "auth-tools"), ("openapi-2", "data-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
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
        SetupListOpenApiCollectionsQuery();

        var command = StartInteractiveCommand(
            "openapi",
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
        SetupListOpenApiCollectionsQuery();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
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

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListOpenApiCollectionsQuery(
            cursor: "cursor-1",
            items: [("openapi-1", "auth-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
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
                  "id": "openapi-1",
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
        SetupListOpenApiCollectionsQuery(
            endCursor: "cursor-2",
            hasNextPage: true,
            items: [("openapi-1", "auth-tools"), ("openapi-2", "data-tools")]);

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "openapi-1",
                  "name": "auth-tools"
                },
                {
                  "id": "openapi-2",
                  "name": "data-tools"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Fact]
    public async Task ListOpenApiCollectionsThrows_ReturnsError()
    {
        // arrange
        SetupListOpenApiCollectionsQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "openapi",
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
