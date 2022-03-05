using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Execution.Serialization;
using HotChocolate.Utilities;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.ConnectionContextKeys;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class ApolloSubscriptionProtocolHandler : IProtocolHandler
{
    private readonly JsonQueryResultSerializer _serializer = new();
    private readonly ISocketSessionInterceptor _interceptor;

    public ApolloSubscriptionProtocolHandler(ISocketSessionInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public string Name => GraphQL_WS;

    public async Task OnReceiveAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        ISocketConnection connection = session.Connection;

        var connected = connection.ContextData.ContainsKey(Connected);

        if (connected && message.IsSingleSegment &&
            message.First.Equals(Utf8MessageBodies.KeepAlive))
        {
            // received a simple ping, we do not need to answer to this message.
            return;
        }

        using var document = JsonDocument.Parse(message);
        JsonElement root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseAsync(
                "The message must be a json object.",
                CloseReasons.InvalidMessage,
                cancellationToken);
            return;
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out JsonElement type) ||
            type.ValueKind is not JsonValueKind.String)
        {
            await connection.CloseAsync(
                "The type property on the message is obligatory.",
                CloseReasons.InvalidMessage,
                cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.ConnectionInitialize))
        {
            if (connected)
            {
                await connection.CloseAsync(
                    "Too many initialisation requests.",
                    ConnectionCloseReason.ProtocolError,
                    cancellationToken);
                return;
            }

            InitializeConnectionMessage operationMessageObj =
                TryGetPayload(root, out JsonElement payload)
                    ? new InitializeConnectionMessage(payload)
                    : InitializeConnectionMessage.Default;

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
                await SendConnectionRejectMessage(
                    session,
                    connectionStatus.Message,
                    connectionStatus.Extensions,
                    cancellationToken);

                await connection.CloseAsync(
                    connectionStatus.Message,
                    ConnectionCloseReason.NormalClosure,
                    cancellationToken);
            }
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Start))
        {
            if (!ParseSubscribeMessage(root, out DataStartMessage? subscribeMessage))
            {
                await connection.CloseAsync(
                    "Invalid subscribe message structure.",
                    CloseReasons.InvalidMessage,
                    cancellationToken);
                return;
            }

            if (!session.Operations.Register(subscribeMessage.Id, subscribeMessage.Payload))
            {
                await connection.CloseAsync(
                    "The subscription id is not unique.",
                    CloseReasons.InvalidMessage,
                    cancellationToken);
                return;
            }

            // the operation is registered and we are done.
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Stop))
        {
            if (root.TryGetProperty(Utf8MessageProperties.Id, out JsonElement idProp) &&
                idProp.ValueKind is JsonValueKind.String)
            {
                session.Operations.Unregister(idProp.GetString()!);
            }
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.ConnectionTerminate))
        {
            await _interceptor.OnCloseAsync(session, cancellationToken);

            await connection.CloseAsync(
                TerminateConnectionMessageHandler_Message,
                ConnectionCloseReason.NormalClosure,
                cancellationToken);
            return;
        }

        await connection.CloseAsync(
            "Invalid message type.",
            CloseReasons.InvalidMessage,
            cancellationToken);
    }

    public async Task SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => await session.Connection.SendAsync(Utf8MessageBodies.KeepAlive, cancellationToken);

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
        jsonWriter.WriteString("type", Utf8Messages.Data);
        jsonWriter.WritePropertyName("payload");
        _serializer.Serialize(result, jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.Body, cancellationToken);
    }

    public async Task SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("id", operationSessionId);
        jsonWriter.WriteString("type", Utf8Messages.Error);
        jsonWriter.WritePropertyName("payload");
        _serializer.Serialize(errors[0], jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.Body, cancellationToken);
    }

    public async Task SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.Complete, id: operationSessionId);
        await session.Connection.SendAsync(writer.Body, cancellationToken);
    }

    private static async Task SendConnectionAcceptMessage(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.ConnectionAccept, payload);
        await session.Connection.SendAsync(writer.Body, cancellationToken);
    }

    private static async Task SendConnectionRejectMessage(
        ISocketSession session,
        string message,
        IReadOnlyDictionary<string, object?>? extensions,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("type", Utf8Messages.ConnectionError);
        jsonWriter.WritePropertyName("payload");
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("message", message);
        jsonWriter.WritePropertyName("extensions");
        JsonSerializer.Serialize(jsonWriter, extensions);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.Body, cancellationToken);
    }

    private static bool ParseSubscribeMessage(
        JsonElement messageElement,
        [NotNullWhen(true)] out DataStartMessage? message)
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

        message = new DataStartMessage(id, request[0]);
        return true;
    }
}
