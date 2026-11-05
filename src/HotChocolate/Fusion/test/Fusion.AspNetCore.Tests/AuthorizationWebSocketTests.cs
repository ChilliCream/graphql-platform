using System.Net.WebSockets;
using System.Text;
using HotChocolate.Fusion.Policies;
using HotChocolate.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AuthorizationWebSocketTests : FusionTestBase
{
    private const string Schema =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        type Query {
          field: String @authenticated
        }
        """;

    [Fact]
    public async Task
        SendConnectionInitAsync_Should_AcknowledgeTheConnection_When_AnonymousAndRejectedOnUnauthenticated()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieScheme,
            configureGatewayBuilder: b => b.ModifyPolicyOptions(
                o => o.RejectRequestOn = RejectRequestOn.OnUnauthenticated));
        using var webSocket = await ConnectAsync(gateway);

        // act
        await SendConnectionInitAsync(webSocket);
        var message = await ReceiveTextAsync(webSocket);

        // assert
        Assert.Equal("""{"type":"connection_ack"}""", message);
    }

    [Fact]
    public async Task SendConnectionInitAsync_Should_AcknowledgeTheConnection_When_AnonymousAndNotRejected()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieScheme);
        using var webSocket = await ConnectAsync(gateway);

        // act
        await SendConnectionInitAsync(webSocket);
        var message = await ReceiveTextAsync(webSocket);

        // assert
        Assert.Equal("""{"type":"connection_ack"}""", message);
    }

    private static void AddCookieScheme(IServiceCollection services)
        => services.AddAuthentication().AddCookie("Cookies");

    private static async Task<WebSocket> ConnectAsync(Gateway gateway)
    {
        var webSocketClient = gateway.CreateWebSocketClient();
        webSocketClient.ConfigureRequest =
            r => r.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

        return await webSocketClient.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
    }

    private static async Task SendConnectionInitAsync(WebSocket webSocket)
        => await webSocket.SendAsync(
            Encoding.UTF8.GetBytes("""{"type":"connection_init"}"""),
            WebSocketMessageType.Text,
            endOfMessage: true,
            TestContext.Current.CancellationToken);

    private static async Task<string> ReceiveTextAsync(WebSocket webSocket)
    {
        var buffer = new byte[1024];
        var received = await webSocket.ReceiveAsync(buffer, TestContext.Current.CancellationToken);

        return Encoding.UTF8.GetString(buffer, 0, received.Count);
    }
}
