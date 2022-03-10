using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client.Helpers;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class DataMessageObserver : IObserver<IOperationMessage>, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly ConcurrentQueue<IDataMessage> _messages = new();
    private readonly string _id;

    public DataMessageObserver(string id)
    {
        _id = id;
    }

    public async ValueTask<IDataMessage?> TryReadNextAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        _messages.TryDequeue(out IDataMessage? message);
        return message;
    }

    public void OnNext(IOperationMessage value)
    {
        if (value is IDataMessage message && message.Id.EqualsOrdinal(_id))
        {
            _messages.Enqueue(message);
            _semaphore.Release();
        }
    }

    public void OnError(Exception error) { }

    public void OnCompleted()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
