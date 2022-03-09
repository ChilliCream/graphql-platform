using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class GraphQLOverWebSocketProtocolHandler : IProtocolHandler
{
    public string Name { get; }

    public ValueTask InitializeAsync<T>(
        SocketClientContext context,
        T payload,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<ResultStream> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public ValueTask OnReceiveAsync(
        SocketClientContext context,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
