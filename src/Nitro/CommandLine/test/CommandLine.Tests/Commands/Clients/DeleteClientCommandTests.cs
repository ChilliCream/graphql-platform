using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class DeleteClientCommandTests
{
    [Fact]
    public async Task Delete_MissingId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "delete",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The client ID is required in non-interactive mode.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_WithIdAndForce_JsonOutput_ReturnsDeletedClient()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.DeleteClientAsync("client-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDeleteClientResult("client-1", "web-client", "Products API"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "delete",
            "client-1",
            "--force",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            {
              "id": "client-1",
              "name": "web-client",
              "api": {
                "name": "Products API"
              }
            }
            """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IClientsClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IClientsClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static IDeleteClientByIdCommandMutation_DeleteClientById CreateDeleteClientResult(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>();
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([]);

        var client = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById_Client_Client>();
        client.SetupGet(x => x.Id).Returns(id);
        client.SetupGet(x => x.Name).Returns(name);
        client.SetupGet(x => x.Api).Returns(api.Object);

        var payload = new Mock<IDeleteClientByIdCommandMutation_DeleteClientById>();
        payload.SetupGet(x => x.Client).Returns(client.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
