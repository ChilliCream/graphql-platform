using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ListClientCommandTests
{
    [Fact]
    public async Task List_MissingApiId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "--output",
            "json");

        // assert
        Assert.NotEqual(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            The API ID is required in non-interactive mode.
            """);
        Assert.Empty(host.StdErr);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task List_NonInteractive_JsonOutput_ReturnsPaginatedResult()
    {
        // arrange
        var page = new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(
            [CreateClientNode("client-1", "web-client", "Products API")],
            EndCursor: "cursor-1",
            HasNextPage: true);

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListClientsAsync(
                "api-1",
                "cursor-start",
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "--api-id",
            "api-1",
            "--cursor",
            "cursor-start",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "values": [
                {
                  "id": "client-1",
                  "name": "web-client",
                  "api": {
                    "name": "Products API"
                  }
                }
              ],
              "cursor": "cursor-1"
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task List_InteractivePath_WhenSelectionIsCancelled_ReturnsCancelledExitCode()
    {
        // arrange
        var page = new ConnectionPage<IListClientCommandQuery_Node_Clients_Edges_Node>(
            [],
            EndCursor: null,
            HasNextPage: false);

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ListClientsAsync(
                "api-1",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var host = CreateHost(client);
        host.Console.Input.PushKey(ConsoleKey.Escape);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "list",
            "--api-id",
            "api-1");

        // assert
        Assert.Equal(1, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IClientsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IClientsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IListClientCommandQuery_Node_Clients_Edges_Node_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>();
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([]);

        var client = new Mock<IListClientCommandQuery_Node_Clients_Edges_Node_Client>();
        client.SetupGet(x => x.Id).Returns(id);
        client.SetupGet(x => x.Name).Returns(name);
        client.SetupGet(x => x.Api).Returns(api.Object);

        return client.Object;
    }
}
