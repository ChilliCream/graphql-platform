using System.Net.WebSockets;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.AspNetCore.Subscriptions;

public class WebSocketConnectionTests(TestServerFactory serverFactory)
    : ServerTestBase(serverFactory)
{
    private static readonly Uri s_subscriptionUri = new("ws://localhost:5000/graphql");

    [Theory]
    [InlineData(new[] { "graphql-transport-ws", "graphql-ws" }, "graphql-transport-ws")]
    [InlineData(new[] { "graphql-ws", "graphql-transport-ws" }, "graphql-ws")]
    [InlineData(new[] { "foo", "graphql-ws" }, "graphql-ws")]
    [InlineData(new[] { "graphql-transport-ws" }, "graphql-transport-ws")]
    [InlineData(new[] { "graphql-ws" }, "graphql-ws")]
    public async Task TryAcceptConnection_Should_AcceptFirstSupportedProtocol_When_AnySupported(
        string[] protocols,
        string expectedProtocol)
    {
        // arrange
        using var testServer = CreateStarWarsServer();
        var client = CreateWebSocketClient(testServer, protocols);

        // act
        using var webSocket = await client.ConnectAsync(
            s_subscriptionUri,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedProtocol, webSocket.SubProtocol);
    }

    [Fact]
    public async Task TryAcceptConnection_Should_CloseWithProtocolError_When_NoneSupported()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var testServer = CreateStarWarsServer();
        var client = CreateWebSocketClient(testServer, ["foo", "bar"]);

        // act
        using var webSocket = await client.ConnectAsync(s_subscriptionUri, ct);
        await webSocket.ReceiveAsync(new byte[1024], ct);

        // assert
        Assert.Equal(WebSocketCloseStatus.ProtocolError, webSocket.CloseStatus);
    }

    private static WebSocketClient CreateWebSocketClient(
        TestServer testServer,
        string[] protocols)
    {
        var client = testServer.CreateWebSocketClient();

        foreach (var protocol in protocols)
        {
            client.SubProtocols.Add(protocol);
        }

        return client;
    }
}
