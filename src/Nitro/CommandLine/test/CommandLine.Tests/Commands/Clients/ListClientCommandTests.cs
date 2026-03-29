using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Exceptions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "client",
                "list",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Lists all clients of an API

            Usage:
                            nitro client list [command] [options]

            Options:
                            --api-id <api-id>        The ID of the API [env: NITRO_API_ID]
                            --cursor <cursor>        The cursor to start the query (non interactive mode) [env: NITRO_CURSOR]
                            --cloud-url <cloud-url>  The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
                            --api-key <api-key>      The API key that is used for the authentication [env: NITRO_API_KEY]
                            --output <json>          The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
                            -?, -h, --help           Show help and usage information

            Commands:
                            versions            Lists all versions of a client
                            published-versions  Lists all published versions of a client
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
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Fact]
    public async Task NoWorkspaceInSession_And_NoApiId_ReturnsError_Interactive()
    {
        // arrange & act
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);

        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddSession()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or manually specify the '--workspace-id' option (if available).
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingApiId_ReturnsError(InteractionMode mode)
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
                "list")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The '--api-id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products"),
                ("client-2", "mobile-client", "products")));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess(
            """

                                             Clients of API

                                      ┌──────────┬───────────────┐
                                      │ Id       │ Name          │
                                      ├──────────┼───────────────┤
                                      │ client-1 │ web-client    │
                                      │ client-2 │ mobile-client │
                                      └──────────┴───────────────┘
                                             Clients of API

                                      ┌──────────┬───────────────┐
                                      │ Id       │ Name          │
                                      ├──────────┼───────────────┤
                                      │ client-1 │ web-client    │
                                      │ client-2 │ mobile-client │
                                      └──────────┴───────────────┘
                                             Clients of API

                                      ┌──────────┬───────────────┐
                                      │ Id       │ Name          │
                                      ├──────────┼───────────────┤
                                      │ client-1 │ web-client    │
                                      │ client-2 │ mobile-client │
                                      └──────────┴───────────────┘
            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithApiId_ReturnSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products")));

        var command = new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .Start();

        // act
        command.SelectOption(0);
        var result = await command.RunToCompletionAsync();

        // assert
        result.AssertSuccess(
            """

                                             Clients of API

                                       ┌──────────┬────────────┐
                                       │ Id       │ Name       │
                                       ├──────────┼────────────┤
                                       │ client-1 │ web-client │
                                       └──────────┴────────────┘
                                             Clients of API

                                       ┌──────────┬────────────┐
                                       │ Id       │ Name       │
                                       ├──────────┼────────────┤
                                       │ client-1 │ web-client │
                                       └──────────┴────────────┘
                                             Clients of API

                                       ┌──────────┬────────────┐
                                       │ Id       │ Name       │
                                       ├──────────┼────────────┤
                                       │ client-1 │ web-client │
                                       └──────────┴────────────┘
            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "products"
              }
            }
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task WithCursor_ReturnSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                "cursor-1",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage(
                endCursor: null,
                hasNextPage: false,
                ("client-1", "web-client", "products")));

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1",
                "--cursor",
                "cursor-1")
            .ExecuteAsync();

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

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientException("list failed"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: list failed
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientAuthorizationException("forbidden"), "api-1", null);

        // act
        var result = await new CommandBuilder()
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    private static ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node> CreateListClientsPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, string ApiName)[] clients)
    {
        var items = clients
            .Select(static client => CreateClientNode(client.Id, client.Name, client.ApiName))
            .ToArray();

        return new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(items, endCursor, hasNextPage);
    }

    private static IListClientCommandQuery_Node_Clients_Edges_Node CreateClientNode(
        string id,
        string name,
        string apiName)
     {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>(MockBehavior.Strict);
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns(["products"]);

        var clientNode = new Mock<IListClientCommandQuery_Node_Clients_Edges_Node>(MockBehavior.Strict);
        clientNode.SetupGet(x => x.Id).Returns(id);
        clientNode.SetupGet(x => x.Name).Returns(name);
        clientNode.SetupGet(x => x.Api).Returns(api.Object);
        clientNode.SetupGet(x => x.Versions).Returns((IShowClientCommandQuery_Node_Versions?)null);

        return clientNode.Object;
     }

    private static Mock<IClientsClient> CreateListExceptionClient(
        Exception ex,
        string apiId,
        string? cursor)
     {
         var client = new Mock<IClientsClient>(MockBehavior.Strict);
         client.Setup(x => x.ListClientsAsync(
                 apiId,
                 cursor,
                 10,
                 It.IsAny<CancellationToken>()))
             .ThrowsAsync(ex);
         return client;
     }
 }
