using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.TestHost;

namespace HotChocolate.Fusion;

/// <summary>
/// A raw graphql-transport-ws client that reports every frame as text.
/// </summary>
public sealed class SubscriptionWebSocketClient : IDisposable
{
    private readonly WebSocket _socket;

    private SubscriptionWebSocketClient(WebSocket socket) => _socket = socket;

    public static async Task<SubscriptionWebSocketClient> ConnectAsync(
        WebSocketClient client,
        string? user = null,
        string? expiry = null)
    {
        client.ConfigureRequest = request =>
        {
            request.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;

            if (user is not null)
            {
                request.Headers[SubscriptionPoliciesTransportTestBase.UserHeader] = user;
            }

            if (expiry is not null)
            {
                request.Headers[SubscriptionPoliciesTransportTestBase.ExpiryHeader] = expiry;
            }
        };

        var socket = await client.ConnectAsync(
            new Uri("ws://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        return new SubscriptionWebSocketClient(socket);
    }

    /// <summary>
    /// Sends <c>connection_init</c> and returns the next frame, which is <c>connection_ack</c> for an
    /// accepted connection.
    /// </summary>
    public async Task<string> InitializeAsync()
    {
        await SendAsync("""{"type":"connection_init"}""");

        return await ReceiveAsync();
    }

    public Task SendSubscribeAsync(string id, string query)
        => SendAsync(
            JsonSerializer.Serialize(
                new { id, type = "subscribe", payload = new { query } }));

    public async Task SendAsync(string message)
        => await _socket.SendAsync(
            Encoding.UTF8.GetBytes(message),
            WebSocketMessageType.Text,
            endOfMessage: true,
            TestContext.Current.CancellationToken);

    /// <summary>
    /// Receives the next frame as indented JSON, or as <c>close code description</c> for a close frame.
    /// </summary>
    public async Task<string> ReceiveAsync()
    {
        var buffer = new byte[16 * 1024];
        await using var message = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await _socket.ReceiveAsync(buffer, TestContext.Current.CancellationToken);

            if (result.MessageType is WebSocketMessageType.Close)
            {
                return $"close {(int)result.CloseStatus!.Value} {result.CloseStatusDescription}";
            }

            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return WireJson.Indent(Encoding.UTF8.GetString(message.ToArray()));
    }

    public void Dispose() => _socket.Dispose();
}
