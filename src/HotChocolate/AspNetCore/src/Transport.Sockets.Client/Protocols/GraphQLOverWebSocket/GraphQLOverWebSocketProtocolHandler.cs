using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        var observer = new ConnectionMessageObserver(cancellationToken);
        using IDisposable subscription = context.Messages.Subscribe(observer);
        await context.Socket.SendConnectionInitMessage(payload, cancellationToken);
        await observer.Accepted;
    }

    public IAsyncEnumerable<OperationResult> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request)
        => new OperationExecutor(context, request);

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
}
