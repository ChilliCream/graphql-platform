using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public sealed class ListPersonalAccessTokenCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "pat",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all personal access tokens

            Usage:
              nitro pat list [options]

            Options:
              --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
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

        var command = new CommandBuilder()
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
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          │ pat-2 │ ci-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          │ pat-2 │ ci-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          │ pat-2 │ ci-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiKey_ReturnsSuccess_NonInteractive()
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
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
    public async Task WithApiKey_ReturnsSuccess_JsonOutput()
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
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

        var command = new CommandBuilder()
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
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                          PersonalAccessTokens

                        There was no data found.
                          PersonalAccessTokens

                        There was no data found.
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithApiKey_NoData_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
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
    public async Task WithApiKey_NoData_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var client = new Mock<IPersonalAccessTokensClient>(MockBehavior.Strict);
        client.Setup(x => x.ListPersonalAccessTokensAsync(
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListPage());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
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

        var command = new CommandBuilder()
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
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
                                  PersonalAccessTokens

                          ┌───────┬─────────────┬────────────┐
                          │ Id    │ Description │ Expires in │
                          ├───────┼─────────────┼────────────┤
                          │ pat-1 │ my-token    │ Expired    │
                          └───────┴─────────────┴────────────┘
            {
              "id": "pat-1",
              "description": "my-token",
              "createdAt": "2025-01-01T00:00:00+00:00",
              "expiresAt": "2025-06-01T00:00:00+00:00"
            }
            """);
        Assert.Equal(0, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
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

    [Fact]
    public async Task WithCursor_ReturnsSuccess_JsonOutput()
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
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
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

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateListExceptionClient(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments("pat", "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
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
