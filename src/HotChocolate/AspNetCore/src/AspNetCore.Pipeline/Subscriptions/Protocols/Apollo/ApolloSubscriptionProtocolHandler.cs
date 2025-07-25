using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.Buffers;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.MessageProperties;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;
using static HotChocolate.Language.Utf8GraphQLRequestParser;
using static HotChocolate.Transport.Sockets.WellKnownProtocols;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class ApolloSubscriptionProtocolHandler : IProtocolHandler
{
    private readonly ISocketSessionInterceptor _interceptor;
    private readonly IWebSocketPayloadFormatter _formatter;

    public ApolloSubscriptionProtocolHandler(
        ISocketSessionInterceptor interceptor,
        IWebSocketPayloadFormatter formatter)
    {
        _interceptor = interceptor;
        _formatter = formatter;
    }

    public string Name => GraphQL_WS;

    public async ValueTask OnReceiveAsync(
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
        var connection = session.Connection;
        var connected = connection.IsConnected;

        if (connected && message.IsSingleSegment
            && message.First.Equals(Utf8MessageBodies.KeepAlive))
        {
            // received a simple ping, we do not need to answer to this message.
            return;
        }

        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        JsonElement idProp;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseAsync(
                Apollo_OnReceive_MessageMustBeJson,
                CloseReasons.InvalidMessage,
                cancellationToken);
            return;
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out var type)
            || type.ValueKind is not JsonValueKind.String)
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

            var operationMessageObj =
                TryGetPayload(root, out var payload)
                    ? new InitializeConnectionMessage(payload)
                    : InitializeConnectionMessage.Default;

            var connectionStatus =
                await _interceptor.OnConnectAsync(
                    session,
                    operationMessageObj,
                    cancellationToken);

            if (connectionStatus.Accepted)
            {
                connection.IsConnected = true;
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
                if (!TryParseSubscribeMessage(root, out var dataStartMessage))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_InvalidSubscribeMessage,
                        CloseReasons.InvalidMessage,
                        cancellationToken);
                    return;
                }

                if (!session.Operations.Enqueue(dataStartMessage.Id, dataStartMessage.Payload))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_SubscriptionIdNotUnique,
                        ConnectionCloseReason.InternalServerError,
                        cancellationToken);
                }
            }
            catch (GraphQLRequestException ex)
            {
                if (!root.TryGetProperty(Id, out idProp)
                    || idProp.ValueKind is not JsonValueKind.String
                    || string.IsNullOrEmpty(idProp.GetString()))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_InvalidMessageType,
                        CloseReasons.InvalidMessage,
                        cancellationToken);
                    return;
                }

                await SendErrorMessageAsync(
                    session,
                    idProp.GetString()!,
                    ex.Errors,
                    cancellationToken);
            }
            catch (SyntaxException ex)
            {
                if (!root.TryGetProperty(Id, out idProp)
                    || idProp.ValueKind is not JsonValueKind.String
                    || string.IsNullOrEmpty(idProp.GetString()))
                {
                    await connection.CloseAsync(
                        Apollo_OnReceive_InvalidMessageType,
                        CloseReasons.InvalidMessage,
                        cancellationToken);
                    return;
                }

                var syntaxError = new Error
                {
                    Message = ex.Message,
                    Locations = [new Location(ex.Line, ex.Column)]
                };

                await SendErrorMessageAsync(
                    session,
                    idProp.GetString()!,
                    [syntaxError],
                    cancellationToken);
            }

            // the operation is registered and we are done.
            return;
        }

        if (connected && type.ValueEquals(Utf8Messages.Stop))
        {
            if (root.TryGetProperty(Utf8MessageProperties.Id, out idProp)
                && idProp.ValueKind is JsonValueKind.String
                && idProp.GetString() is { Length: > 0 }
                id)
            {
                session.Operations.Complete(id);
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

    public ValueTask SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => session.Connection.SendAsync(Utf8MessageBodies.KeepAlive, cancellationToken);

    public async ValueTask SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IOperationResult result,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new PooledArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Id, operationSessionId);
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.Data);
        jsonWriter.WritePropertyName(Payload);
        _formatter.Format(result, jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.WrittenMemory, cancellationToken);
    }

    public async ValueTask SendErrorMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IReadOnlyList<IError> errors,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new PooledArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Id, operationSessionId);
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.Error);
        jsonWriter.WritePropertyName(Payload);
        _formatter.Format(errors[0], jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.WrittenMemory, cancellationToken);
    }

    public async ValueTask SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken)
    {
        using var writer = new PooledArrayWriter();
        SerializeMessage(writer, _formatter, Utf8Messages.Complete, id: operationSessionId);
        await session.Connection.SendAsync(writer.WrittenMemory, cancellationToken);
    }

    private async ValueTask SendConnectionAcceptMessage(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new PooledArrayWriter();
        SerializeMessage(writer, _formatter, Utf8Messages.ConnectionAccept, payload);
        await session.Connection.SendAsync(writer.WrittenMemory, cancellationToken);
    }

    private async ValueTask SendConnectionRejectMessage(
        ISocketSession session,
        string message,
        IReadOnlyDictionary<string, object?>? extensions,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new PooledArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.ConnectionError);
        jsonWriter.WritePropertyName(Payload);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Message, message);
        jsonWriter.WritePropertyName(MessageProperties.Extensions);

        if (extensions is null)
        {
            jsonWriter.WriteNullValue();
        }
        else
        {
            _formatter.Format(extensions, jsonWriter);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.WrittenMemory, cancellationToken);
    }

    public ValueTask OnConnectionInitTimeoutAsync(
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
        if (!messageElement.TryGetProperty(Id, out var idProp)
            || idProp.ValueKind is not JsonValueKind.String
            || string.IsNullOrEmpty(idProp.GetString()))
        {
            message = null;
            return false;
        }

        if (!messageElement.TryGetProperty(Payload, out var payloadProp)
            || payloadProp.ValueKind is not JsonValueKind.Object)
        {
            message = null;
            return false;
        }

        var id = idProp.GetString()!;
        var request = Parse(payloadProp.GetRawText());

        if (request.Count == 0)
        {
            message = null;
            return false;
        }

        message = new DataStartMessage(id, request[0]);
        return true;
    }
}
