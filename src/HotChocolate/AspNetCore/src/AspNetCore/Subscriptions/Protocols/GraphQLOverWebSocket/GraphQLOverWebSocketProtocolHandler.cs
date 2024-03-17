using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution.Serialization;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Transport.Sockets.WellKnownProtocols;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;
using static HotChocolate.AspNetCore.Subscriptions.ConnectionContextKeys;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket.MessageProperties;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IGraphQLOverWebSocketProtocolHandler
{
    private readonly JsonResultFormatter _formatter = new();
    private readonly ISocketSessionInterceptor _interceptor;

    public GraphQLOverWebSocketProtocolHandler(ISocketSessionInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public string Name => GraphQL_Transport_WS;

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
            await session.Connection.CloseUnexpectedErrorAsync(cancellationToken);
        }
    }

    private async ValueTask OnReceiveInternalAsync(
        ISocketSession session,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken)
    {
        var connection = session.Connection;

        var connected = connection.ContextData.ContainsKey(Connected);
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        JsonElement idProp;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            await connection.CloseMessageMustBeJsonObjectAsync(cancellationToken);
            return;
        }

        if (!root.TryGetProperty(Utf8MessageProperties.Type, out var type) ||
            type.ValueKind is not JsonValueKind.String)
        {
            await connection.CloseMessageTypeIsMandatoryAsync(cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.Ping))
        {
            var operationMessageObj =
                TryGetPayload(root, out var payload)
                    ? new PingMessage(payload)
                    : PingMessage.Default;

            var responsePayload =
                await _interceptor.OnPingAsync(session, operationMessageObj, cancellationToken);

            await SendPongMessageAsync(session, responsePayload, cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.Pong))
        {
            var operationMessageObj =
                TryGetPayload(root, out var payload)
                    ? new PongMessage(payload)
                    : PongMessage.Default;

            await _interceptor.OnPongAsync(session, operationMessageObj, cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.ConnectionInitialize))
        {
            if (connected)
            {
                await connection.CloseToManyInitializationsAsync(cancellationToken);
                return;
            }

            var operationMessageObj =
                TryGetPayload(root, out var payload)
                    ? new ConnectionInitMessage(payload)
                    : ConnectionInitMessage.Default;

            var connectionStatus =
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
                await connection.CloseConnectionRefusedAsync(cancellationToken);
            }
            return;
        }

        // if we have not received a connection init and connection was successfully accepted
        // then we will close the connection with a unauthorized error.
        if (!connected)
        {
            await connection.CloseUnauthorizedAsync(cancellationToken);
            return;
        }

        if (type.ValueEquals(Utf8Messages.Subscribe))
        {
            try
            {
                if (!TryParseSubscribeMessage(root, out var subscribeMessage))
                {
                    await connection.CloseInvalidSubscribeMessageAsync(cancellationToken);
                    return;
                }

                if (!session.Operations.Enqueue(subscribeMessage.Id, subscribeMessage.Payload))
                {
                    await connection.CloseSubscriptionIdNotUniqueAsync(cancellationToken);
                    return;
                }
            }
            catch (GraphQLRequestException ex)
            {
                if (!root.TryGetProperty(Id, out idProp) ||
                    idProp.ValueKind is not JsonValueKind.String ||
                    string.IsNullOrEmpty(idProp.GetString()))
                {
                    await connection.CloseInvalidSubscribeMessageAsync(cancellationToken);
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
                if (!root.TryGetProperty(Id, out idProp) ||
                    idProp.ValueKind is not JsonValueKind.String ||
                    string.IsNullOrEmpty(idProp.GetString()))
                {
                    await connection.CloseInvalidSubscribeMessageAsync(cancellationToken);
                    return;
                }

                var syntaxError = new Error(
                    ex.Message,
                    locations: new[]
                    {
                        new Location(ex.Line, ex.Column),
                    });

                await SendErrorMessageAsync(
                    session,
                    idProp.GetString()!,
                    new[] { syntaxError, },
                    cancellationToken);
            }

            // the operation was excepted and we are done.
            return;
        }

        if (type.ValueEquals(Utf8Messages.Complete) &&
            root.TryGetProperty(Id, out idProp) &&
            idProp.ValueKind is JsonValueKind.String &&
            idProp.GetString() is { Length: > 0, } id)
        {
            session.Operations.Complete(id);
            return;
        }

        await connection.CloseInvalidMessageTypeAsync(cancellationToken);
    }

    public ValueTask SendKeepAliveMessageAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => SendPingMessageAsync(session, null, cancellationToken);

    public async ValueTask SendResultMessageAsync(
        ISocketSession session,
        string operationSessionId,
        IOperationResult result,
        CancellationToken cancellationToken)
    {
        using var arrayWriter = new ArrayWriter();
        await using var jsonWriter = new Utf8JsonWriter(arrayWriter, WriterOptions);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString(Id, operationSessionId);
        jsonWriter.WriteString(MessageProperties.Type, Utf8Messages.Next);
        jsonWriter.WritePropertyName(Payload);
        _formatter.Format(result, jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.GetWrittenMemory(), cancellationToken);
    }

    public async ValueTask SendErrorMessageAsync(
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
        _formatter.FormatErrors(errors, jsonWriter);
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync(cancellationToken);
        await session.Connection.SendAsync(arrayWriter.GetWrittenMemory(), cancellationToken);
    }

    public async ValueTask SendCompleteMessageAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.Complete, id: operationSessionId);
        await session.Connection.SendAsync(writer.GetWrittenMemory(), cancellationToken);
    }

    public async ValueTask SendPingMessageAsync(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            await session.Connection.SendAsync(Utf8MessageBodies.DefaultPing, cancellationToken);
        }
        else
        {
            using var writer = new ArrayWriter();
            SerializeMessage(writer, Utf8Messages.Ping, payload);
            await session.Connection.SendAsync(writer.GetWrittenMemory(), cancellationToken);
        }
    }

    private static async ValueTask SendPongMessageAsync(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            await session.Connection.SendAsync(Utf8MessageBodies.DefaultPong, cancellationToken);
        }
        else
        {
            using var writer = new ArrayWriter();
            SerializeMessage(writer, Utf8Messages.Pong, payload);
            await session.Connection.SendAsync(writer.GetWrittenMemory(), cancellationToken);
        }
    }

    private static async ValueTask SendConnectionAcceptMessage(
        ISocketSession session,
        IReadOnlyDictionary<string, object?>? payload,
        CancellationToken cancellationToken)
    {
        using var writer = new ArrayWriter();
        SerializeMessage(writer, Utf8Messages.ConnectionAccept, payload);
        await session.Connection.SendAsync(writer.GetWrittenMemory(), cancellationToken);
    }

    public ValueTask OnConnectionInitTimeoutAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => session.Connection.CloseConnectionInitTimeoutAsync(cancellationToken);

    private static bool TryParseSubscribeMessage(
        JsonElement messageElement,
        [NotNullWhen(true)] out SubscribeMessage? message)
    {
        if (!messageElement.TryGetProperty(Id, out var idProp) ||
            idProp.ValueKind is not JsonValueKind.String ||
            string.IsNullOrEmpty(idProp.GetString()))
        {
            message = null;
            return false;
        }

        if (!messageElement.TryGetProperty(Payload, out var payloadProp) ||
            payloadProp.ValueKind is not JsonValueKind.Object)
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

        DefaultHttpRequestParser.EnsureValidQueryId(request);
        message = new SubscribeMessage(id, request[0]);
        return true;
    }
}
