using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client;

public interface ISocketClient : IAsyncDisposable
{
    IAsyncEnumerable<OperationResult> ExecuteAsync(OperationRequest request);
}
