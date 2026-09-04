using System.Net.WebSockets;
using System.Text;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AspNetCore.Subscriptions;

public class WebSocketConnectionTests : ServerTestBase
{
    public WebSocketConnectionTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Theory]
    [InlineData("graphql-transport-ws")]
    [InlineData("graphql-ws")]
    public async Task ReadMessageAsync_Should_CloseWithMessageTooBig_When_MessageExceedsMaxAllowedMessageSize(
        string protocol)
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = cts.Token;
        using var testServer = CreateStarWarsServer(
            configureConventions: mapping => mapping.WithOptions(
                new GraphQLServerOptions
                {
                    Sockets =
                    {
                        MaxAllowedMessageSize = 1024
                    }
                }));
        var client = testServer.CreateWebSocketClient();
        client.SubProtocols.Add(protocol);
        using var webSocket = await client.ConnectAsync(new Uri("ws://localhost:5000/graphql"), ct);

        // act
        var padding = new string('X', 64 * 1024);
        var message = Encoding.UTF8.GetBytes(
            $$$"""{"type":"connection_init","payload":{"data":"{{{padding}}}"}}""");
        await webSocket.SendAsync(message, WebSocketMessageType.Text, endOfMessage: true, ct);
        await webSocket.ReceiveAsync(new byte[4096], ct);

        // assert
        Assert.Equal(WebSocketCloseStatus.MessageTooBig, webSocket.CloseStatus);
    }
}
