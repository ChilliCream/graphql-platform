namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all clients of an API.

            Usage:
                            nitro client list [command] [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
                            --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information

            Commands:
                            versions            List all versions of a client.
                            published-versions  List all published versions of a client.

            Example:
              nitro client list --api-id "<api-id>"
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
            "client",
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
        SetupInteractionMode(InteractionMode.Interactive);
        SetupSession();

        var result = await ExecuteCommandAsync(
            "client",
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
            "client",
            "list");

        // assert
        result.AssertError(
            """
            Missing required option '--api-id'.
            """);
    }

    [Fact]
    public async Task WithApiId_ReturnSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientsQuery(
            clients: [(ClientId, ClientName, ApiName),
                ("client-2", "mobile-client", ApiName)]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientsQuery(
            clients: [(ClientId, ClientName, ApiName)]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "client-1",
                  "name": "web-client",
                  "api": {
                    "name": "products"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientsQuery(
            cursor: "cursor-1",
            clients: [(ClientId, ClientName, ApiName)]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "--api-id",
            ApiId,
            "--cursor",
            "cursor-1");

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientsQuery(
            cursor: "cursor-1",
            clients: [(ClientId, ClientName, ApiName)]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
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
                  "id": "client-1",
                  "name": "web-client",
                  "api": {
                    "name": "products"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task ListClientsThrows_ReturnsError()
    {
        // arrange
        SetupListClientsQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """

            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task List_Should_ReturnSuccess_When_NoClientsReturned_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientsQuery();

        var command = StartInteractiveCommand(
            "client",
            "list",
            "--api-id",
            ApiId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task List_Should_ReturnError_When_ApiNotFound()
    {
        // arrange
        SetupListClientsQueryNotFound();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            The API was not found.
            """);
    }
}
