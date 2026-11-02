using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    // The close code the client uses when the server sends a message that violates the
    // graphql-transport-ws protocol. It mirrors the code the server itself uses for the
    // same condition.
    private const WebSocketCloseStatus InvalidMessage = (WebSocketCloseStatus)4400;

    public string Name => WellKnownProtocols.GraphQL_Transport_WS;

    public async ValueTask InitializeAsync(
        SocketClientContext context,
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        if (payload.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            throw new ArgumentException(
                "The payload must be an object, null, or undefined.",
                nameof(payload));
        }

        using var observer = new ConnectionMessageObserver<ConnectionAcceptMessage>(
            context.Socket,
            cancellationToken);
        using var subscription = context.Messages.Subscribe(observer);
        await context.Socket.SendConnectionInitMessage(payload, cancellationToken);
        await observer.Accepted;
    }

    public async ValueTask<SocketResult> ExecuteAsync(
        SocketClientContext context,
        IOperationRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var observer = new DataMessageObserver(id);
        var completion = new DataCompletion(context.Socket, id);
        var subscription = context.Messages.Subscribe(observer);

        try
        {
            await context.Socket.SendSubscribeMessageAsync(id, request, cancellationToken);

            // if the user cancels this stream, we send the server a complete request so that we
            // no longer receive new result messages, and we complete the local observer so that a
            // pending read terminates gracefully instead of blocking forever.
            void OnCancelled()
            {
                completion.TrySendCompleteMessage();
                observer.OnCompleted();
            }

            var cancellationRegistration = cancellationToken.Register(OnCancelled);

            return new SocketResult(observer, subscription, completion, cancellationRegistration);
        }
        catch
        {
            // the subscribe send (or the cancellation registration) faulted, so the observer was
            // never handed to a SocketResult. Release it and remove it from the message stream.
            subscription.Dispose();
            observer.Dispose();
            throw;
        }
    }

    public async ValueTask<SocketResult> ExecuteBatchAsync(
        SocketClientContext context,
        OperationBatchRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var observer = new DataMessageObserver(id);
        var completion = new DataCompletion(context.Socket, id);
        var subscription = context.Messages.Subscribe(observer);

        try
        {
            await context.Socket.SendSubscribeMessageAsync(id, request, cancellationToken);

            // if the user cancels this stream, we send the server a complete request so that we
            // no longer receive new result messages, and we complete the local observer so that a
            // pending read terminates gracefully instead of blocking forever.
            void OnCancelled()
            {
                completion.TrySendCompleteMessage();
                observer.OnCompleted();
            }

            var cancellationRegistration = cancellationToken.Register(OnCancelled);

            return new SocketResult(observer, subscription, completion, cancellationRegistration);
        }
        catch
        {
            // the subscribe send (or the cancellation registration) faulted, so the observer was
            // never handed to a SocketResult. Release it and remove it from the message stream.
            subscription.Dispose();
            observer.Dispose();
            throw;
        }
    }

    public ValueTask OnReceiveAsync(
        SocketClientContext context,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default)
    {
        // A malformed server frame must not fault the shared message pipeline. Any parsing
        // or structural failure while classifying or reading the message is treated as a
        // protocol violation and closes the socket with code 4400 instead of propagating.
        try
        {
            switch (ParseMessageType(message))
            {
                case MessageType.Ping:
                    return context.Socket.SendPongMessageAsync(cancellationToken);

                case MessageType.Pong:
                    // we do nothing and just accept the pong as a valid message.
                    return default;

                case MessageType.Next:
                    context.Messages.OnNext(NextMessage.From(message));
                    return default;

                case MessageType.Error:
                    context.Messages.OnNext(ErrorMessage.From(message));
                    return default;

                case MessageType.Complete:
                    context.Messages.OnNext(CompleteMessage.From(message));
                    return default;

                case MessageType.ConnectionAccept:
                    context.Messages.OnNext(ConnectionAcceptMessage.Default);
                    return default;

                default:
                    return FatalError(context, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return FatalError(context, cancellationToken);
        }

        static async ValueTask FatalError(
            SocketClientContext context,
            CancellationToken cancellationToken = default)
        {
            const string reason = "Invalid message structure.";

            // Surface the error to consumers first so a pending ReadResultsAsync unblocks
            // immediately, then close the socket. Channel completion is idempotent, so a
            // later close-triggered SocketClosedException is a harmless no-op.
            context.Messages.OnError(new SocketClosedException(reason, InvalidMessage));
            await context.Socket.CloseAsync(InvalidMessage, reason, cancellationToken);
        }
    }

    private static MessageType ParseMessageType(ReadOnlySequence<byte> message)
    {
        var reader = new Utf8JsonReader(message);

        while (reader.Read())
        {
            if (reader.CurrentDepth == 1
                && reader.TokenType == JsonTokenType.PropertyName
                && reader.ValueTextEquals(Utf8MessageProperties.TypeProp))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                {
                    // The top-level type must be a string. Anything else is not a valid
                    // message type, so keep scanning rather than misclassifying it.
                    continue;
                }

                if (reader.ValueTextEquals(Utf8Messages.Ping))
                {
                    return MessageType.Ping;
                }

                if (reader.ValueTextEquals(Utf8Messages.Pong))
                {
                    return MessageType.Pong;
                }

                if (reader.ValueTextEquals(Utf8Messages.Next))
                {
                    return MessageType.Next;
                }

                if (reader.ValueTextEquals(Utf8Messages.Error))
                {
                    return MessageType.Error;
                }

                if (reader.ValueTextEquals(Utf8Messages.Complete))
                {
                    return MessageType.Complete;
                }

                if (reader.ValueTextEquals(Utf8Messages.ConnectionAccept))
                {
                    return MessageType.ConnectionAccept;
                }

                return MessageType.None;
            }
        }

        return MessageType.None;
    }

    private sealed class DataCompletion(WebSocket socket, string id) : IDataCompletion
    {
        private int _completed;

        public void MarkDataStreamCompleted()
            => Interlocked.Exchange(ref _completed, 1);

        public void TrySendCompleteMessage()
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
            {
                _ = TrySendCompleteMessageInternalAsync(socket, id);
            }
        }
    }

    private static async Task TrySendCompleteMessageInternalAsync(WebSocket socket, string id)
    {
        using var cts = new CancellationTokenSource(2000);

        try
        {
            if (socket.IsOpen())
            {
                await socket.SendCompleteMessageAsync(id, cts.Token);
            }
        }
        catch
        {
            // if we cannot send the complete message we will just abort the socket.
            try
            {
                socket.Abort();
            }
            catch
            {
                // ignore
            }
        }
    }

    private enum MessageType
    {
        None,
        Ping,
        Pong,
        Next,
        Error,
        Complete,
        ConnectionAccept
    }
}
