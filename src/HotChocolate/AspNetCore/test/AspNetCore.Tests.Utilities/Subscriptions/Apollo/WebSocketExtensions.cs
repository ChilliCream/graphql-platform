using System.Net.WebSockets;
using System.Text;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Sockets;
using HotChocolate.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Tests.Utilities.Subscriptions.Apollo;

public static class WebSocketExtensions
{
    private static readonly JsonSerializerSettings _settings =
        new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

    public static Task SendConnectionInitializeAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
        => SendMessageAsync(
            webSocket,
            new InitializeConnectionMessage(),
            cancellationToken: cancellationToken);

    public static async Task SendConnectionInitializeAsync(
        this WebSocket webSocket,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.ConnectionInitialize, payload);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static async Task SendTerminateConnectionAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.ConnectionTerminate);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static async Task SendSubscriptionStartAsync(
        this WebSocket webSocket,
        string subscriptionId,
        GraphQLRequest request,
        bool largeMessage = false)
    {
        await SendMessageAsync(
           webSocket,
           new DataStartMessage(subscriptionId, request),
           largeMessage);
    }

    public static async Task SendSubscriptionStopAsync(
        this WebSocket webSocket,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        MessageUtilities.SerializeMessage(writer, Utf8Messages.Stop, id: subscriptionId);
        await SendMessageAsync(webSocket, writer.GetWrittenMemory(), cancellationToken);
    }

    public static Task SendMessageAsync(
        this WebSocket webSocket,
        string message,
        CancellationToken cancellationToken)
        => webSocket.SendAsync(
            Encoding.UTF8.GetBytes(message),
            WebSocketMessageType.Text,
            true,
            cancellationToken);

    public static async Task SendMessageAsync(
        this WebSocket webSocket,
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
        => await webSocket.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);

    public static async Task SendMessageAsync(
        this WebSocket webSocket,
        OperationMessage message,
        bool largeMessage = false,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[SocketDefaults.BufferSize];

        await using var stream = message.CreateMessageStream(largeMessage);
        int read;

        do
        {
            read = await stream.ReadAsync(buffer, cancellationToken);
            var segment = new ArraySegment<byte>(buffer, 0, read);
            var isEndOfMessage = stream.Position == stream.Length;

            await webSocket.SendAsync(
                segment,
                WebSocketMessageType.Text,
                isEndOfMessage,
                cancellationToken);
        } while (read == SocketDefaults.BufferSize);
    }

    private static Stream CreateMessageStream(this OperationMessage message, bool largeMessage)
    {
        if (message is DataStartMessage dataStart)
        {
            var query = dataStart.Payload.Query!.Print();

            var payload = new Dictionary<string, object> { { "query", query }, };

            if (dataStart.Payload.QueryId != null)
            {
                payload["namedQuery"] = dataStart.Payload.QueryId;
            }

            if (dataStart.Payload.OperationName != null)
            {
                payload["operationName"] = dataStart.Payload.OperationName;
            }

            if (dataStart.Payload.Variables != null)
            {
                payload["variables"] = dataStart.Payload.Variables;
            }

            message = new HelperOperationMessage(
                dataStart.Type, dataStart.Id, payload);
        }

        var json = JsonConvert.SerializeObject(message, _settings);
        if (largeMessage)
        {
            json += new string(' ', 1024 * 16);
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    public static async Task<IReadOnlyDictionary<string, object?>?> ReceiveServerMessageAsync(
        this WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        var buffer = new byte[SocketDefaults.BufferSize];
        bool skipped;

        do
        {
            var array = new ArraySegment<byte>(buffer);
            result = await webSocket.ReceiveAsync(array, cancellationToken);

            if (result.Count == 2 && result.EndOfMessage &&
                buffer.AsSpan()[..2].SequenceEqual(Utf8MessageBodies.KeepAlive.Span))
            {
                skipped = true;
                continue;
            }

            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            skipped = false;
        }
        while (!result.EndOfMessage || skipped);

        if (stream.Length == 0)
        {
            return null;
        }

        return (IReadOnlyDictionary<string, object?>?)ParseJson(stream.ToArray())!;
    }

    private sealed class HelperOperationMessage : OperationMessage
    {
        public HelperOperationMessage(string type, string id, object payload)
            : base(type)
        {
            Id = id;
            Payload = payload;
        }

        public string Id { get; }

        public object Payload { get; }
    }
}
