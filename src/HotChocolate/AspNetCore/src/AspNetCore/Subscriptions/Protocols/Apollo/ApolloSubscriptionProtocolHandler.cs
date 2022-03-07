using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Execution.Serialization;
using HotChocolate.Utilities;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.AspNetCore.Subscriptions.ConnectionContextKeys;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.MessageProperties;
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
        try
        {
            await OnReceiveInternalAsync(session, message, cancellationToken);
        }
        catch
        {
            await session.Connection.CloseAsync(
                "Unexpected Error",
                ConnectionCloseReason.InternalServerError,
                cancellationToken);
        }
    }

    private async ValueTask OnReceiveInternalAsync(
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
        JsonElement idProp;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseAsync(
                Apollo_OnReceive_MessageMustBeJson,
                CloseReasons.InvalidMessage,
                cancellationToken);
            return;
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out JsonElement type) ||
            type.ValueKind is not JsonValueKind.String)
        {
            await connection.CloseAsync(
                Apollo_OnReceive_TypePropMissing,
                CloseReasons.InvalidMessage,
                cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.ConnectionInitialize))
        {
            if (connected)
            {
                await connection.CloseAsync(
                    Apollo_OnReceive_ToManyInitializations,
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
            try
            {
                if (!TryParseSubscribeMessage(root, out DataStartMessage? dataStartMessage))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_InvalidSubscribeMessage,
                        CloseReasons.InvalidMessage,
                        cancellationToken);
                    return;
                }

                if (!session.Operations.Register(dataStartMessage.Id, dataStartMessage.Payload))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_SubscriptionIdNotUnique,
                        ConnectionCloseReason.InternalServerError,
                        cancellationToken);
                    return;
                }
            }
            catch (SyntaxException ex)
            {
                if (!root.TryGetProperty(Id, out idProp) ||
                    idProp.ValueKind is not JsonValueKind.String ||
                    string.IsNullOrEmpty(idProp.GetString()))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_InvalidMessageType,
                        CloseReasons.InvalidMessage,
                        cancellationToken);
                    return;
                }

                var syntaxError = new Error(
                    ex.Message,
                    locations: new[]
                    {
                        new Location(ex.Line, ex.Column)
                    });

                await SendErrorMessageAsync(
                    session,
                    idProp.GetString()!,
                    new[] { syntaxError },
                    cancellationToken);
            }

            // the operation is registered and we are done.
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Stop))
        {
            if (root.TryGetProperty(Utf8MessageProperties.Id, out idProp) &&
                idProp.ValueKind is JsonValueKind.String &&
                idProp.GetString() is { Length: > 0 } id)
            {
                session.Operations.Unregister(id);
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
            Apollo_OnReceive_InvalidMessageType,
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
        jsonWriter.WriteString(Id, operationSessionId);
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.Data);
        jsonWriter.WritePropertyName(Payload);
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
        jsonWriter.WriteString(Id, operationSessionId);
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.Error);
        jsonWriter.WritePropertyName(Payload);
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
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.ConnectionError);
        jsonWriter.WritePropertyName(Payload);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Message, message);
        jsonWriter.WritePropertyName(MessageProperties.Extensions);
        JsonSerializer.Serialize(jsonWriter, extensions);
        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.Body, cancellationToken);
    }

    public Task OnConnectionInitTimeoutAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => session.Connection.CloseAsync(
            "Connection initialization timeout.",
            ConnectionCloseReason.ProtocolError,
            cancellationToken);

    private static bool TryParseSubscribeMessage(
        JsonElement messageElement,
        [NotNullWhen(true)] out DataStartMessage? message)
    {
        if (!messageElement.TryGetProperty(Id, out JsonElement idProp) ||
            idProp.ValueKind is not JsonValueKind.String ||
            string.IsNullOrEmpty(idProp.GetString()))
        {
            message = null;
            return false;
        }

        if (!messageElement.TryGetProperty(Payload, out JsonElement payloadProp) ||
            payloadProp.ValueKind is not JsonValueKind.Object)
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
