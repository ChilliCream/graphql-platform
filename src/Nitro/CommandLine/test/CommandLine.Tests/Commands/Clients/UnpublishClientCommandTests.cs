using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class UnpublishClientCommandTests
{
    [Fact]
    public async Task Unpublish_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "unpublish");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--stage' is required.
            Option '--client-id' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unpublish_SingleTag_UsesClient()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "prod",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishResult("version-1", "web-client"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "unpublish",
            "--client-id",
            "client-1",
            "--stage",
            "prod",
            "--tag",
            "v1");

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Unpublish_MultipleTags_UsesClientForEachTag()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "prod",
                "v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishResult("version-1", "web-client"));

        client.Setup(x => x.UnpublishClientVersionAsync(
                "client-1",
                "prod",
                "v2",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUnpublishResult("version-2", "web-client"));

        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "unpublish",
            "--client-id",
            "client-1",
            "--stage",
            "prod",
            "--tag",
            "v1",
            "v2");

        // assert
        Assert.Equal(0, exitCode);
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

    private static IUnpublishClient_UnpublishClient CreateUnpublishResult(
        string versionId,
        string clientName)
    {
        var client = new Mock<IUnpublishClient_UnpublishClient_ClientVersion_Client_Client>();
        client.SetupGet(x => x.Name).Returns(clientName);

        var version = new Mock<IUnpublishClient_UnpublishClient_ClientVersion_ClientVersion>();
        version.SetupGet(x => x.Id).Returns(versionId);
        version.SetupGet(x => x.Client).Returns(client.Object);

        var result = new Mock<IUnpublishClient_UnpublishClient>();
        result.SetupGet(x => x.ClientVersion).Returns(version.Object);
        result.SetupGet(x => x.Errors).Returns([]);

        return result.Object;
    }
}
