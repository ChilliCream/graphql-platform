using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class ListPersonalAccessTokenCommandTests(NitroCommandFixture fixture)
    : PersonalAccessTokensCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "pat",
            "list",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              List all personal access tokens.

            Usage:
              nitro pat list [options]

            Options:
              --cursor <cursor>        The pagination cursor to resume from [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro pat list
            """);
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListPersonalAccessTokensQuery(
            tokens:
            [
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)),
                ("pat-2", "ci-token", new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero))
            ]);

        // act
        var command = StartInteractiveCommand(
            "pat",
            "list");

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListPersonalAccessTokensQuery(
            endCursor: "cursor-2",
            hasNextPage: true,
            tokens:
            [
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)),
                ("pat-2", "ci-token", new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero))
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "list");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "pat-1",
                  "description": "my-token",
                  "createdAt": "2025-01-01T00:00:00+00:00",
                  "expiresAt": "2025-06-01T00:00:00+00:00"
                },
                {
                  "id": "pat-2",
                  "description": "ci-token",
                  "createdAt": "2025-02-01T00:00:00+00:00",
                  "expiresAt": "2025-07-01T00:00:00+00:00"
                }
              ],
              "cursor": "cursor-2"
            }
            """);
    }

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        SetupInteractionMode(InteractionMode.Interactive);
        SetupListPersonalAccessTokensQuery();

        // act
        var command = StartInteractiveCommand(
            "pat",
            "list");

        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupListPersonalAccessTokensQuery();

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "list");

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
        SetupListPersonalAccessTokensQuery(
            cursor: "cursor-1",
            tokens:
            [
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
            ]);

        // act
        var command = StartInteractiveCommand(
            "pat",
            "list",
            "--cursor",
            "cursor-1");

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
        SetupListPersonalAccessTokensQuery(
            cursor: "cursor-1",
            tokens:
            [
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
            ]);

        // act
        var result = await ExecuteCommandAsync(
            "pat",
            "list",
            "--cursor",
            "cursor-1");

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "id": "pat-1",
                  "description": "my-token",
                  "createdAt": "2025-01-01T00:00:00+00:00",
                  "expiresAt": "2025-06-01T00:00:00+00:00"
                }
              ],
              "cursor": null
            }
            """);
    }

    [Fact]
    public async Task ListPersonalAccessTokensThrows_ReturnsError()
    {
        // arrange
        SetupListPersonalAccessTokensQueryException();

        // act
        var result = await ExecuteCommandAsync("pat", "list");

        // assert
        result.AssertError(
            """
            There was an unexpected error: Something unexpected happened.
            """);
    }
}
