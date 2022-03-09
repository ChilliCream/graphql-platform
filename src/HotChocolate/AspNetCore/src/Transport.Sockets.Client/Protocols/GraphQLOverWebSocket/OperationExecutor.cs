using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets.Client.Helpers;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal sealed class OperationExecutor : IAsyncEnumerable<OperationResult>
{
    private readonly string _id = Guid.NewGuid().ToString("N");
    private readonly SocketClientContext _context;
    private readonly OperationRequest _request;

    public OperationExecutor(SocketClientContext context, OperationRequest request)
    {
        _context = context;
        _request = request;
    }

    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        IDataMessage? message;
        using var observer = new DataMessageObserver(_id);
        using IDisposable subscription = _context.Messages.Subscribe(observer);

        await _context.Socket.SendSubscribeMessageAsync(_id, _request, cancellationToken);

        // if the user cancels this stream we will send the server a complete request
        // so that we no longer receive new result messages.
        cancellationToken.Register(BeginComplete);

        do
        {
            message = await observer.TryReadNextAsync(cancellationToken);

            switch (message)
            {
                case NextMessage next:
                    yield return next.Payload;
                    break;

                case ErrorMessage error:
                    yield return error.Payload;
                    break;

                case CompleteMessage:
                    message = null;
                    break;
            }

        } while (!cancellationToken.IsCancellationRequested && message is not null);

        void BeginComplete()
            => Task.Factory.StartNew(
                async () => await _context.Socket.SendCompleteMessageAsync(
                    _id,
                    CancellationToken.None),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default);
    }

    private sealed class DataMessageObserver : IObserver<IOperationMessage>, IDisposable
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
}
