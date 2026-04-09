namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class ListMockCommandTests(NitroCommandFixture fixture) : MocksCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "mock",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all mock schemas in an API.

            Usage:
              nitro mock list [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
                            --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
                            --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information

            Example:
              nitro mock list --api-id "<api-id>"
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
            "mock",
            "list");

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange
        SetupSession();
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
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
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "list");

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);
    }

    [Fact]
    public async Task ListMockSchemasThrows_ReturnsError()
    {
        // arrange
        SetupListMockSchemasQueryException();

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithApiId_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupListMockSchemasQuery(
            null,
            (MockSchemaId, "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)),
            ("mock-2", "Mock Two", "https://mock.example.com/2", new Uri("https://downstream.example.com/2"),
                "user3", new DateTimeOffset(2025, 2, 10, 10, 0, 0, TimeSpan.Zero),
                "user4", new DateTimeOffset(2025, 2, 11, 10, 0, 0, TimeSpan.Zero)));

        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "mock",
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
        SetupListMockSchemasQuery(
            null,
            (MockSchemaId, "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)),
            ("mock-2", "Mock Two", "https://mock.example.com/2", new Uri("https://downstream.example.com/2"),
                "user3", new DateTimeOffset(2025, 2, 10, 10, 0, 0, TimeSpan.Zero),
                "user4", new DateTimeOffset(2025, 2, 11, 10, 0, 0, TimeSpan.Zero)));

        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
            "list",
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "mock-1",
                  "name": "Mock One",
                  "url": "https://mock.example.com/1",
                  "downstreamUrl": "https://downstream.example.com/1",
                  "createdBy": {
                    "username": "user1",
                    "createdAt": "2025-01-15T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user2",
                    "modifiedAt": "2025-01-16T10:00:00+00:00"
                  }
                },
                {
                  "id": "mock-2",
                  "name": "Mock Two",
                  "url": "https://mock.example.com/2",
                  "downstreamUrl": "https://downstream.example.com/2",
                  "createdBy": {
                    "username": "user3",
                    "createdAt": "2025-02-10T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user4",
                    "modifiedAt": "2025-02-11T10:00:00+00:00"
                  }
                }
              ],
              "cursor": null
            }
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupListMockSchemasQueryWithPagination(
            "cursor-1",
            "cursor-2",
            true,
            (MockSchemaId, "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)));

        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
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
                  "id": "mock-1",
                  "name": "Mock One",
                  "url": "https://mock.example.com/1",
                  "downstreamUrl": "https://downstream.example.com/1",
                  "createdBy": {
                    "username": "user1",
                    "createdAt": "2025-01-15T10:00:00+00:00"
                  },
                  "modifiedBy": {
                    "username": "user2",
                    "modifiedAt": "2025-01-16T10:00:00+00:00"
                  }
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Fact]
    public async Task WithApiId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupListMockSchemasQuery();
        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "mock",
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
        SetupListMockSchemasQuery();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "mock",
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
        SetupListMockSchemasQuery(
            "cursor-1",
            (MockSchemaId, "Mock One", "https://mock.example.com/1", new Uri("https://downstream.example.com/1"),
                "user1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero),
                "user2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero)));

        SetupInteractionMode(InteractionMode.Interactive);

        var command = StartInteractiveCommand(
            "mock",
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
}
