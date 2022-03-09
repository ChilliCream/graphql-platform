using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal interface IProtocolHandler
{
    string Name { get; }

    ValueTask InitializeAsync<T>(
        SocketClientContext context,
        T payload,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<OperationResult> ExecuteAsync(
        SocketClientContext context,
        OperationRequest request);

    ValueTask OnReceiveAsync(
        SocketClientContext context,
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken = default);
}
