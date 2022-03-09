using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client;

internal interface IProtocolHandler
{
    string Name { get; }

    ValueTask InitializeAsync<T>(
        SocketClientContext context,
        T payload,
        CancellationToken cancellationToken = default);

    ValueTask<ResultStream> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask OnReceiveAsync(
        SocketClientContext context,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);
}
