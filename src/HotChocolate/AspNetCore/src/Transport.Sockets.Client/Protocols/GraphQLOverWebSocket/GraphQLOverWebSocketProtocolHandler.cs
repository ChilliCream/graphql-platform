using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    public string Name => WellKnownProtocols.GraphQL_Transport_WS;

    public async ValueTask InitializeAsync<T>(
        SocketClientContext context,
        T payload,
        CancellationToken cancellationToken = default)
    {
        var observer = new ConnectionMessageObserver<ConnectionAcceptMessage>(cancellationToken);
        using var subscription = context.Messages.Subscribe(observer);
        await context.Socket.SendConnectionInitMessage(payload, cancellationToken);
        await observer.Accepted;
    }

    public async ValueTask<SocketResult> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var observer = new DataMessageObserver(id);
        var completion = new DataCompletion(context.Socket, id);
        var subscription = context.Messages.Subscribe(observer);

        await context.Socket.SendSubscribeMessageAsync(id, request, cancellationToken);

        // if the user cancels this stream we will send the server a complete request
        // so that we no longer receive new result messages.
        cancellationToken.Register(completion.TrySendCompleteMessage);

        try
        {
            return new SocketResult(observer, subscription, completion);
        }
        catch
        {
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

        static async ValueTask FatalError(
            SocketClientContext context,
            CancellationToken cancellationToken = default)
        {
            context.Messages.OnCompleted();
            await context.Socket.CloseAsync(
                WebSocketCloseStatus.ProtocolError,
                "Invalid Message Structure",
                cancellationToken);
        }
    }

    private static MessageType ParseMessageType(ReadOnlySequence<byte> message)
    {
        var reader = new Utf8JsonReader(message);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName &&
                reader.ValueTextEquals(Utf8MessageProperties.TypeProp))
            {
                reader.Read();

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
        private bool _completed;

        public void MarkDataStreamCompleted()
            => _completed = true;

        public void TrySendCompleteMessage()
        {
            if (!_completed)
            {
                Task.Factory.StartNew(
                    async () =>
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
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);
                _completed = true;
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
        ConnectionAccept,
    }
}
