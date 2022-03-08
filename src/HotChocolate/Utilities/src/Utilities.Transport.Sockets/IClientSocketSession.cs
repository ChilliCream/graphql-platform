using System.Net.WebSockets;

namespace HotChocolate.Utilities.Transport.Sockets;

public interface IClientSocketSession : IAsyncDisposable
{
    WebSocket Socket { get; }

    ValueTask InitializeAsync<T>(T payload, CancellationToken cancellationToken = default);

    IAsyncEnumerable<object> ExecuteAsync(OperationRequest request);
}
