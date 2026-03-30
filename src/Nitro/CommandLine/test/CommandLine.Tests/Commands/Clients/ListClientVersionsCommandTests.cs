using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientVersionsCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "list",
                "versions",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all versions of a client

            Usage:
              nitro client list versions [options]

            Options:
              --client-id <client-id>  The ID of the client [env: NITRO_CLIENT_ID]
              --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
              --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "list",
                "versions")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingClientId_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "list",
                "versions")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--client-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" }),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero), new[] { "staging" })));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "production"
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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" }),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero), new[] { "staging" })));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "production"
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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_NoData_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_NoData_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage());

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" }),
                ("v2", new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero), new[] { "staging" })));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      │ v2  │ 2025-01-16 10:00:00Z │ staging    │
                      └─────┴──────────────────────┴────────────┘
                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      │ v2  │ 2025-01-16 10:00:00Z │ staging    │
                      └─────┴──────────────────────┴────────────┘
                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      │ v2  │ 2025-01-16 10:00:00Z │ staging    │
                      └─────┴──────────────────────┴────────────┘
            {
              "tag": "v1",
              "createdAt": "2025-01-15T10:00:00+00:00",
              "stages": [
                "production"
              ]
            }
            """);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithClientId_NoData_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage());

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        Assert.Empty(result.StdErr);
        result.StdOut.MatchInlineSnapshot(
            """

                          Versions

                There was no data found.
                          Versions

                There was no data found.
            """);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" })));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1",
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

                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      └─────┴──────────────────────┴────────────┘
                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      └─────┴──────────────────────┴────────────┘
                                        Versions

                      ┌─────┬──────────────────────┬────────────┐
                      │ Tag │ Created              │ Stages     │
                      ├─────┼──────────────────────┼────────────┤
                      │ v1  │ 2025-01-15 10:00:00Z │ production │
                      └─────┴──────────────────────┴────────────┘
            {
              "tag": "v1",
              "createdAt": "2025-01-15T10:00:00+00:00",
              "stages": [
                "production"
              ]
            }
            """);
        Assert.Equal(0, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" })));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """

            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "production"
                  ]
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientVersionsAsync(
                "client-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientVersionsPage(
                endCursor: null,
                hasNextPage: false,
                ("v1", new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero), new[] { "production" })));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "values": [
                {
                  "tag": "v1",
                  "createdAt": "2025-01-15T10:00:00+00:00",
                  "stages": [
                    "production"
                  ]
                }
              ],
              "cursor": null
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientException("list failed"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: list failed
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientException("list failed"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: list failed
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientException("list failed"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientAuthorizationException("forbidden"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientAuthorizationException("forbidden"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(
            new NitroClientAuthorizationException("forbidden"), "client-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "versions",
                "--client-id",
                "client-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    private static ConnectionPage<IClientDetailPrompt_ClientVersionEdge> CreateListClientVersionsPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Tag, DateTimeOffset CreatedAt, string[] Stages)[] versions)
    {
        var items = versions
            .Select(static v => CreateVersionEdge(v.Tag, v.CreatedAt, v.Stages))
            .ToArray();

        return new ConnectionPage<IClientDetailPrompt_ClientVersionEdge>(items, endCursor, hasNextPage);
    }

    private static IClientDetailPrompt_ClientVersionEdge CreateVersionEdge(
        string tag,
        DateTimeOffset createdAt,
        string[] stages)
    {
        var publishedTo = stages
            .Select(static stageName =>
            {
                var stage = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo_Stage>(MockBehavior.Strict);
                stage.SetupGet(x => x.Name).Returns(stageName);

                var published = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node_PublishedTo>(MockBehavior.Strict);
                published.SetupGet(x => x.Stage).Returns(stage.Object);

                return published.Object;
            })
            .ToArray();

        var node = new Mock<IShowClientCommandQuery_Node_Versions_Edges_Node>(MockBehavior.Strict);
        node.SetupGet(x => x.Tag).Returns(tag);
        node.SetupGet(x => x.CreatedAt).Returns(createdAt);
        node.SetupGet(x => x.PublishedTo).Returns(publishedTo);

        var edge = new Mock<IClientDetailPrompt_ClientVersionEdge>(MockBehavior.Strict);
        edge.SetupGet(x => x.Node).Returns(node.Object);

        return edge.Object;
    }

    private static Mock<IClientsClient> CreateListExceptionClient(
        Exception ex,
        string clientId,
        string? cursor)
    {
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListClientVersionsAsync(
                clientId,
                cursor,
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
