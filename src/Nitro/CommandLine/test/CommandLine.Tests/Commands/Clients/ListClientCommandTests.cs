using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "client",
                "list",
                "--help")
            .ExecuteAsync();

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
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
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

        var result = await new CommandBuilder(fixture)
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

        var result = await new CommandBuilder(fixture)
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithApiId_ReturnSuccess(InteractionMode mode)
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
        var result = await new CommandBuilder(fixture)
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

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithCursor_ReturnSuccess(InteractionMode mode)
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
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
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
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientGraphQLException("Some message.", "SOME_CODE"), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
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
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(clientsClient.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "client",
                "list",
                "--api-id",
                "api-1")
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

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = CreateListExceptionClient(new NitroClientAuthorizationException(), "api-1", null);

        // act
        var result = await new CommandBuilder(fixture)
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

    [Fact]
    public async Task List_Should_ReturnSuccess_When_NoClientsReturned_Interactive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateListClientsPage());

        var command = new CommandBuilder(fixture)
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
        result.AssertSuccess();

        apisClient.VerifyAll();
        clientsClient.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task List_Should_ReturnError_When_ApiNotFound(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>?)null);

        // act
        var result = await new CommandBuilder(fixture)
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
            There was an issue with the request to the server.
            The API was not found.
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
