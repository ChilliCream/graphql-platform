namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientPublishedVersionsCommandTests(NitroCommandFixture fixture)
    : ClientsCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all published versions of a client.

            Usage:
              nitro client list published-versions [options]

            Options:
              --client-id <client-id>  The ID of the client [env: NITRO_CLIENT_ID]
              --stage <stage>          The name of the stage [env: NITRO_STAGE]
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro client list published-versions \
                --client-id "<client-id>" \
                --stage "dev"
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
            "published-versions",
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            Missing required option '--client-id'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingStage_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        SetupInteractionMode(mode);

        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing required option '--stage'.
            """);
        Assert.Equal(1, result.ExitCode);
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
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithClientId_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientPublishedVersionsQuery(
            versions: [
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero))]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "publishedAt": "2025-01-15T10:00:00+00:00"
                },
                {
                  "tag": "v2",
                  "publishedAt": "2025-01-16T10:00:00+00:00"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task WithClientId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientPublishedVersionsQuery(
            versions: [
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero))]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithClientId_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListClientPublishedVersionsQuery();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

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
    public async Task WithClientId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListClientPublishedVersionsQuery();

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

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
        SetupListClientPublishedVersionsQuery(
            cursor: "cursor-1",
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero))]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
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
        SetupListClientPublishedVersionsQuery(
            cursor: "cursor-1",
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero))]);

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage,
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "publishedAt": "2025-01-15T10:00:00+00:00"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task PublishedVersions_Should_PromptForStage_When_ClientProvided_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupGetClientApiId();
        SetupListStagesQuery(("stage-1", Stage));
        SetupListClientPublishedVersionsQuery(
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero))]);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId);

        // act
        command.SelectOption(0); // Select stage
        command.SelectOption(0); // Select version
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task PublishedVersions_Should_PromptForApiAndClient_When_StageProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListClientPublishedVersionsQuery(
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero))]);

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions",
            "--stage",
            Stage);

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.SelectOption(0); // Select version
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task PublishedVersions_Should_PromptForApiClientAndStage_When_NothingProvided_Interactive()
    {
        // arrange
        SetupSelectApisPrompt((ApiId, "products"));
        SetupListClientsForPrompt((ClientId, ClientName));
        SetupListStagesQuery(("stage-1", Stage));
        SetupListClientPublishedVersionsQuery(
            versions: [("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero))]);

        SetupSessionWithWorkspace();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "client",
            "list",
            "published-versions");

        // act
        command.SelectOption(0); // Select API
        command.SelectOption(0); // Select client
        command.SelectOption(0); // Select stage
        command.SelectOption(0); // Select version
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Fact]
    public async Task ListClientPublishedVersionsThrows_ReturnsError()
    {
        // arrange
        SetupListClientPublishedVersionsQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

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
    public async Task ListPublished_Should_ReturnError_When_ClientNotFound()
    {
        // arrange
        SetupListClientPublishedVersionsQueryNotFound();

        // act
        var result = await ExecuteCommandAsync(
            "client",
            "list",
            "published-versions",
            "--client-id",
            ClientId,
            "--stage",
            Stage);

        // assert
        result.AssertError(
            """
            The client was not found.
            """);
    }
}
