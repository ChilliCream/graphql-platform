using System.Net.WebSockets;
using System.Text;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;
using HotChocolate.Transport.Sockets;
using HotChocolate.Utilities;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.GraphQLOverWebSocket;

public static class WebSocketExtensions
{
    public static Task SendConnectionInitAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
        => SendConnectionInitAsync(webSocket, null, cancellationToken);

    public static async Task SendConnectionInitAsync(
        this WebSocket webSocket,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.ConnectionInitialize, payload);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static async Task SendSubscribeAsync(
        this WebSocket webSocket,
        string subscriptionId,
        SubscribePayload payload,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, object?>();

        if (payload.QueryId is not null)
        {
            map["id"] = payload.QueryId;
        }

        if (payload.Query is not null)
        {
            map["query"] = payload.Query;
        }

        if (payload.OperationName is not null)
        {
            map["operationName"] = payload.OperationName;
        }

        if (payload.Variables is not null)
        {
            map["variables"] = payload.Variables;
        }

        if (payload.Extensions is not null)
        {
            map["extensions"] = payload.Extensions;
        }

        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.Subscribe, map, subscriptionId);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static async Task SendCompleteAsync(
        this WebSocket webSocket,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.Complete, id: subscriptionId);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static Task SendPingAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
        => SendPingAsync(webSocket, null, cancellationToken);

    public static async Task SendPingAsync(
        this WebSocket webSocket,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.Ping, payload);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static Task SendPongAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
        => SendPongAsync(webSocket, null, cancellationToken);

    public static async Task SendPongAsync(
        this WebSocket webSocket,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.Pong, payload);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static Task SendMessageAsync(
        this WebSocket webSocket,
        string message,
        CancellationToken cancellationToken)
        => SendMessageAsync(webSocket, Encoding.UTF8.GetBytes(message), cancellationToken);

    private static async Task SendMessageAsync(
        this WebSocket webSocket,
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
        => await webSocket.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);

    public static async Task<IReadOnlyDictionary<string, object?>?> ReceiveServerMessageAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        var buffer = new byte[SocketDefaults.BufferSize];

        do
        {
            var array = new ArraySegment<byte>(buffer);
            result = await webSocket.ReceiveAsync(array, cancellationToken);
            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
        }
        while (!result.EndOfMessage);

        if (stream.Length == 0)
        {
            return null;
        }

        return (IReadOnlyDictionary<string, object?>?)ParseJson(stream.ToArray());
    }
}
