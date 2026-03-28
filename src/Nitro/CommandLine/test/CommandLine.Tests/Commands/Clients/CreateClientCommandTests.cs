using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class CreateClientCommandTests
{
    [Fact]
    public async Task Create_MissingWorkspaceAndApi_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(clientsClient, apisClient, NoSession());

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "create",
            "--name",
            "web-client",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            You are not logged in. Run `[bold blue]nitro login[/]` to sign in or specify the workspace ID with the --workspace-id option (if available).
            """);
        clientsClient.VerifyNoOtherCalls();
        apisClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithApiId_JsonOutput_ReturnsClient()
    {
        // arrange
        var clientsClient = new Mock<IClientsClient>(MockBehavior.Strict);
        clientsClient.Setup(x => x.CreateClientAsync(
                "api-1",
                "web-client",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreateClientResult("client-1", "web-client", "Products API"));

        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var host = CreateHost(clientsClient, apisClient);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "create",
            "--api-id",
            "api-1",
            "--name",
            "web-client",
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
        clientsClient.VerifyAll();
        apisClient.VerifyNoOtherCalls();
    }

    private static CommandBuilder CreateHost(
        Mock<IClientsClient> clientsClient,
        Mock<IApisClient> apisClient,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IClientsClient>(clientsClient.Object)
            .AddService<IApisClient>(apisClient.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static TestSessionService NoSession() => new();

    private static ICreateClientCommandMutation_CreateClient CreateCreateClientResult(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>();
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([]);

        var client = new Mock<ICreateClientCommandMutation_CreateClient_Client_Client>();
        client.SetupGet(x => x.Id).Returns(id);
        client.SetupGet(x => x.Name).Returns(name);
        client.SetupGet(x => x.Api).Returns(api.Object);

        var payload = new Mock<ICreateClientCommandMutation_CreateClient>();
        payload.SetupGet(x => x.Client).Returns(client.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
