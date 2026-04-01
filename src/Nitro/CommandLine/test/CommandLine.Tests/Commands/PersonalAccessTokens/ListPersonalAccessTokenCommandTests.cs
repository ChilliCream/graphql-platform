using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class ListPersonalAccessTokenCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "pat",
                "list",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage(
                null,
                false,
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)),
                ("pat-2", "ci-token", new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero))));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage(
                "cursor-2",
                true,
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)),
                ("pat-2", "ci-token", new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero))));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "list")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage());

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "list")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiKey_NoData_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage(
                null,
                false,
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))));

        var command = new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "pat",
                "list",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnsSuccess(InteractionMode mode)
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage(
                null,
                false,
                ("pat-1", "my-token", new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "pat",
                "list",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

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

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    private static ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>
        CreateListPage(
            string? endCursor = null,
            bool hasNextPage = false,
            params (string Id, string Description, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)[] tokens)
    {
        var items = tokens
            .Select(static t =>
                (IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node)
                new ListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node_PersonalAccessToken(
                    t.Id, t.Description, t.ExpiresAt, t.CreatedAt))
            .ToArray();

        return new ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>(
            items, endCursor, hasNextPage);
    }

    private static Mock<IPersonalAccessTokensClient> CreateListExceptionClient(Exception ex)
    {
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
