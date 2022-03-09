using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client;

public interface ISocketClient : IAsyncDisposable
{
    ValueTask InitializeAsync<T>(T payload, CancellationToken cancellationToken = default);

    ValueTask<ResultStream> ExecuteAsync(OperationRequest request);
}
