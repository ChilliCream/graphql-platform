using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ShowClientCommandTests
{
    [Fact]
    public async Task Show_MissingId_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "show");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Required argument missing for command: 'show'.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Show_WithData_JsonOutput_ReturnsClient()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync("client-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientNode("client-1", "web-client", "Products API"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "show",
            "client-1",
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

    [Fact]
    public async Task Show_WithoutData_ReturnsSuccessAndErrorMessage()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.ShowClientAsync("client-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IShowClientCommandQuery_Node?)null);

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "show",
            "client-missing");

        // assert
        Assert.Equal(0, exitCode);
        host.Output.Trim().MatchInlineSnapshot(
            """
            ✕ Could not find a client with ID client-missing
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

    private static IShowClientCommandQuery_Node_Client CreateClientNode(
        string id,
        string name,
        string apiName)
    {
        var api = new Mock<IShowClientCommandQuery_Node_Api_1>();
        api.SetupGet(x => x.Name).Returns(apiName);
        api.SetupGet(x => x.Path).Returns([]);

        var client = new Mock<IShowClientCommandQuery_Node_Client>();
        client.SetupGet(x => x.Id).Returns(id);
        client.SetupGet(x => x.Name).Returns(name);
        client.SetupGet(x => x.Api).Returns(api.Object);

        return client.Object;
    }
}
