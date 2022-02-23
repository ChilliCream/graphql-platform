using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket.ConnectionContextKeys;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    private readonly JsonSerializerOptions _options =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    private readonly ISocketSessionInterceptor2 _sessionInterceptor;


    public string Name => GraphQL_Transport_WS;

    public async Task ExecuteAsync(
        ISocketConnection connection,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        var connected = connection.ContextData.ContainsKey(Connected);
        using var document = JsonDocument.Parse(message);
        JsonElement root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseAsync(
                "The message must be a json object.",
                ConnectionCloseReason.ProtocolError,
                cancellationToken);
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out JsonElement type) ||
            type.ValueKind is not JsonValueKind.String)
        {
            await connection.CloseAsync(
                "The type property on the message is obligatory.",
                ConnectionCloseReason.ProtocolError,
                cancellationToken);
        }

        if (type.ValueEquals(Utf8Messages.ConnectionInitialize))
        {
            if (connected)
            {
                await connection.CloseAsync(
                    "Too many initialisation requests.",
                    CloseReasons.TooManyInitAttempts,
                    cancellationToken);
                return;
            }

            ConnectionInitMessage messageObj = ConnectionInitMessage.Default;

            if (root.TryGetProperty(Utf8MessageProperties.Payload, out JsonElement payloadValue) &&
                payloadValue.ValueKind is JsonValueKind.Object)
            {
                messageObj = new ConnectionInitMessage(payloadValue);
            }

            ConnectionStatus connectionStatus =
                await _sessionInterceptor.OnConnectAsync(
                    connection,
                    this,
                    messageObj,
                    cancellationToken);

            if (connectionStatus.Accepted)
            {
                using var writer = new ArrayWriter();
                var acceptMessage = new ConnectionAckMessage(connectionStatus.Extensions);
                JsonSerializer.SerializeToUtf8Bytes(acceptMessage, _options);
                var response = new ArraySegment<byte>(writer.GetInternalBuffer(), 0, writer.Length);
                connection.ContextData.Add(Connected, true);
                await connection.SendAsync(response, cancellationToken);
            }
            else
            {
                // how do we not accept a connection?
            }
            return;
        }

        if (type.ValueEquals(Utf8Messages.Subscribe))
        {

        }

        throw new NotImplementedException();
    }

    private static ConnectionInitMessage DeserializeInitConnMessage(
        GraphQLSocketMessage parsedMessage)
    {
        var reader = new Utf8JsonReader(parsedMessage.Payload);
        var document = JsonDocument.ParseValue(ref reader);

        return parsedMessage.Payload.Length > 0
            ? new ConnectionInitMessage(document.RootElement)
            : new ConnectionInitMessage();
    }


}

internal static class ConnectionContextKeys
{
    public const string Connected = "HotChocolate.WebSockets.graphql-ws.Connected";
}

internal static class CloseReasons
{
    public const int TooManyInitAttempts = 4429;
}

internal static class Utf8MessageProperties
{

    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    public static ReadOnlySpan<byte> Type
        => new[] { (byte)'t', (byte)'y', (byte)'p', (byte)'e' };

    public static ReadOnlySpan<byte> Payload
        => new[] { (byte)'p', (byte)'a', (byte)'y', (byte)'l', (byte)'o', (byte)'a', (byte)'d' };
}
