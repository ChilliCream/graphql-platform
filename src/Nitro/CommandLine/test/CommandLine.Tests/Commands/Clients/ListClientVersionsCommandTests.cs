namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientVersionsCommandTests(NitroCommandFixture fixture) : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all versions of a client.

            Usage:
              nitro client list versions [options]

            Options:
              --client-id <client-id>  The ID of the client [env: NITRO_CLIENT_ID]
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client list versions --client-id "<client-id>"
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
            "list",
            "versions");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingClientId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions");

        // assert
        result.AssertError(
            """
            Missing required option '--client-id'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithClientId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientVersionsQuery(
            versions: [
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { Stage }),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero), new[] { "staging" })]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "dev"
                  ]
                },
                {
                  "tag": "v2",
                  "createdAt": "2025-01-16T10:00:00+00:00",
                  "stages": [
                    "staging"
                  ]
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithClientId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientVersionsQuery();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

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
    public async Task WithClientId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientVersionsQuery(
            versions: [
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { Stage }),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero), new[] { "staging" })]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithClientId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientVersionsQuery();

        var command = StartInteractiveCommand(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientVersionsQuery(
            cursor: "cursor-1",
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { Stage })]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId,
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
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientVersionsQuery(
            cursor: "cursor-1",
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { Stage })]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId,
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "dev"
                  ]
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task ListClientVersionsThrows_ReturnsError()
    {
        // arrange
        SetupListClientVersionsQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

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
    public async Task ListVersions_Should_ReturnError_When_ClientNotFound()
    {
        // arrange
        SetupListClientVersionsQueryNotFound();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "versions",
            "--client-id",
            ClientId);

        // assert
        result.AssertError(
            """
            The client was not found.
            """);
    }
}
