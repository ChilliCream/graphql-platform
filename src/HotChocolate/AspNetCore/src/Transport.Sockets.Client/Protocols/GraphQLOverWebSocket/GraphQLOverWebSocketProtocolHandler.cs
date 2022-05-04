using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
using static HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Utf8MessageProperties;

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
        using IDisposable subscription = context.Messages.Subscribe(observer);
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
        IDisposable subscription = context.Messages.Subscribe(observer);

        await context.Socket.SendSubscribeMessageAsync(id, request, cancellationToken);

        // if the user cancels this stream we will send the server a complete request
        // so that we no longer receive new result messages.
        cancellationToken.Register(completion.TryComplete);

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
        JsonDocument? document = null;

        try
        {
            document = JsonDocument.Parse(message);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty(TypeProp, out JsonElement typeProp))
            {
                if (typeProp.ValueEquals(Utf8Messages.Ping))
                {
                    return context.Socket.SendPongMessageAsync(cancellationToken);
                }
                else if (typeProp.ValueEquals(Utf8Messages.Pong))
                {
                    // we do nothing and just accept the pong as a valid message.
                }
                else if (typeProp.ValueEquals(Utf8Messages.Next))
                {
                    context.Messages.OnNext(NextMessage.From(document));
                    document = null;
                }
                else if (typeProp.ValueEquals(Utf8Messages.Error))
                {
                    context.Messages.OnNext(ErrorMessage.From(document));
                    document = null;
                }
                else if (typeProp.ValueEquals(Utf8Messages.Complete))
                {
                    context.Messages.OnNext(CompleteMessage.From(document));
                    document = null;
                }
                else if (typeProp.ValueEquals(Utf8Messages.ConnectionAccept))
                {
                    context.Messages.OnNext(ConnectionAcceptMessage.Default);
                }
                else
                {
                    return FatalError(context, cancellationToken);
                }
            }
            else
            {
                return FatalError(context, cancellationToken);
            }
        }
        finally
        {
            document?.Dispose();
        }

        return default;

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

    private sealed class DataCompletion : IDataCompletion
    {
        private readonly WebSocket _socket;
        private readonly string _id;
        private bool _completed;

        public DataCompletion(WebSocket socket, string id)
        {
            _socket = socket;
            _id = id;
        }

        public void SetCompleted()
            => _completed = true;

        public void TryComplete()
        {
            if (!_completed)
            {
                Task.Factory.StartNew(
                    async () =>
                    {
                        try
                        {
                            if (_socket.IsOpen())
                            {
                                await _socket.SendCompleteMessageAsync(_id, CancellationToken.None);
                            }
                        }
                        catch
                        {
                            // we ignore any error here.
                            // Most likely the connection is already closed.
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);
                _completed = true;
            }
        }
    }
}
