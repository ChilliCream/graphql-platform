using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Sockets.Client;

public interface ISocketClient : IAsyncDisposable
{
    ValueTask<ResultStream> ExecuteAsync(OperationRequest request, CancellationToken cancellationToken = default);
}

internal sealed class OperationMessage : IDisposable
{
    public string? Id { get; }

    public string Type { get; }

    public JsonElement Content { get; }

    public void Dispose()
    {

    }
}

internal sealed class MessageStream : IObservable<OperationMessage>, IObserver<OperationMessage>
{
    public IDisposable Subscribe(IObserver<OperationMessage> observer)
    {
        throw new NotImplementedException();
    }

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }

    public void OnNext(OperationMessage value)
    {
        throw new NotImplementedException();
    }
}

internal sealed class SocketClientContext
{
    public SocketClientContext(WebSocket socket)
    {
        Socket = socket;
        Messages = new MessageStream();
    }

    public WebSocket Socket { get; }

    public MessageStream Messages { get; }
}
