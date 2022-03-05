using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Execution.Serialization;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket.ConnectionContextKeys;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    private readonly JsonQueryResultSerializer _serializer = new();
    private readonly ISocketSessionInterceptor _interceptor;

    public GraphQLOverWebSocketProtocolHandler(ISocketSessionInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public string Name => GraphQL_Transport_WS;

    public async Task OnReceiveAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        ISocketConnection connection = session.Connection;

        var connected = connection.ContextData.ContainsKey(Connected);
        using var document = JsonDocument.Parse(message);
        JsonElement root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseAsync(
                "The message must be a json object.",
                ConnectionCloseReason.ProtocolError,
                cancellationToken);
            return;
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out JsonElement type) ||
            type.ValueKind is not JsonValueKind.String)
        {
            await connection.CloseAsync(
                "The type property on the message is obligatory.",
                ConnectionCloseReason.ProtocolError,
                cancellationToken);
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Ping))
        {
            PingMessage operationMessageObj =
                TryGetPayload(root, out JsonElement payload)
                    ? new PingMessage(payload)
                    : PingMessage.Default;

            IReadOnlyDictionary<string, object?>? responsePayload =
                await _interceptor.OnPingAsync(session, operationMessageObj, cancellationToken);

            await SendPongMessageAsync(session, responsePayload, cancellationToken);
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Pong))
        {
            PongMessage operationMessageObj =
                TryGetPayload(root, out JsonElement payload)
                    ? new PongMessage(payload)
                    : PongMessage.Default;

            await _interceptor.OnPongAsync(session, operationMessageObj, cancellationToken);
            return;
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

            ConnectionInitMessage operationMessageObj =
                TryGetPayload(root, out JsonElement payload)
                    ? new ConnectionInitMessage(payload)
                    : ConnectionInitMessage.Default;

            ConnectionStatus connectionStatus =
                await _interceptor.OnConnectAsync(
                    session,
                    operationMessageObj,
                    cancellationToken);

            if (connectionStatus.Accepted)
            {
                connection.ContextData.Add(Connected, true);
                await SendConnectionAcceptMessage(
                    session,
                    connectionStatus.Extensions,
                    cancellationToken);
            }
            else
            {
                // how do we not accept a connection?
            }
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Subscribe))
        {
            if (!ParseSubscribeMessage(root, out SubscribeMessage? subscribeMessage))
            {
                await connection.CloseAsync(
                    "Invalid subscribe message structure.",
                    ConnectionCloseReason.ProtocolError,
                    cancellationToken);
                return;
            }

            if (!session.Operations.Register(subscribeMessage.Id, subscribeMessage.Payload))
            {
                await connection.CloseAsync(
                    "The subscription id is not unique.",
                    CloseReasons.SubscriberNotUnique,
                    cancellationToken);
                return;
            }

            // the operation was excepted and we are done.
            return;
        }

        await connection.CloseAsync(
            "Invalid message type.",
            ConnectionCloseReason.ProtocolError,
            cancellationToken);
    }

    public Task SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => SendPongMessageAsync(session, null, cancellationToken);

    public async Task SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IQueryResult result,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("id", operationSessionId);
        jsonWriter.WriteString("type", Utf8Messages.Next);
        jsonWriter.WritePropertyName("payload");
        JsonSerializer.Serialize(jsonWriter, _serializer);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);

        var array = new ArraySegment<byte>(arrayWriter.GetInternalBuffer(), 0, arrayWriter.Length);
        await session.Connection.SendAsync(array, cancellationToken);
    }

    public Task SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.Complete);
        var array = new ArraySegment<byte>(writer.GetInternalBuffer(), 0, writer.Length);
        await session.Connection.SendAsync(array, cancellationToken);
    }

    private static async Task SendPingMessageAsync(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();

        if (payload is null)
        {
            ReadOnlyMemory<byte> message = Utf8MessageBodies.DefaultPing;
            message.CopyTo(writer.GetMemory(message.Length));
        }
        else
        {
            SerializeMessage(writer, Utf8Messages.Pong, payload);
        }

        var array = new ArraySegment<byte>(writer.GetInternalBuffer(), 0, writer.Length);
        await session.Connection.SendAsync(array, cancellationToken);
    }

    private static async Task SendPongMessageAsync(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();

        if (payload is null)
        {
            ReadOnlyMemory<byte> message = Utf8MessageBodies.DefaultPong;
            message.CopyTo(writer.GetMemory(message.Length));
        }
        else
        {
            SerializeMessage(writer, Utf8Messages.Pong, payload);
        }

        var array = new ArraySegment<byte>(writer.GetInternalBuffer(), 0, writer.Length);
        await session.Connection.SendAsync(array, cancellationToken);
    }

    private static async Task SendConnectionAcceptMessage(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.ConnectionAccept, payload);
        var array = new ArraySegment<byte>(writer.GetInternalBuffer(), 0, writer.Length);
        await session.Connection.SendAsync(array, cancellationToken);
    }

    private static bool ParseSubscribeMessage(
        JsonElement messageElement,
        [NotNullWhen(true)] out SubscribeMessage? message)
    {
        if (!messageElement.TryGetProperty("id", out JsonElement idProp) ||
            idProp.ValueKind is not JsonValueKind.String)
        {
            message = null;
            return false;
        }

        if (!messageElement.TryGetProperty("payload", out JsonElement payloadProp) ||
            idProp.ValueKind is not JsonValueKind.Object)
        {
            message = null;
            return false;
        }

        var id = idProp.GetString()!;
        IReadOnlyList<GraphQLRequest> request = Parse(payloadProp.GetRawText());

        if (request.Count == 0)
        {
            message = null;
            return false;
        }

        message = new SubscribeMessage(id, request[0]);
        return true;
    }
}
