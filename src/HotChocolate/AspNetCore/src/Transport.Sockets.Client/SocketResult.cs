using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

public sealed class SocketResult : IDisposable
{
    private readonly ResultEnumerable _enumerable;
    private bool _disposed;

    internal SocketResult(DataMessageObserver observer, IDisposable subscription)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (subscription is null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        _enumerable = new ResultEnumerable(observer, subscription);
    }

    public IAsyncEnumerable<OperationResult> ReadResultsAsync()
        => _enumerable;

    public void Dispose()
    {
        if (!_disposed)
        {
            _enumerable.Dispose();
            _disposed = true;
        }
    }

    private sealed class ResultEnumerable : IAsyncEnumerable<OperationResult>, IDisposable
    {
        private readonly DataMessageObserver _observer;
        private readonly IDisposable _subscription;
        private bool _started;

        public ResultEnumerable(DataMessageObserver observer, IDisposable subscription)
        {
            _observer = observer;
            _subscription = subscription;
        }

        public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_started)
            {
                throw new InvalidOperationException("This stream can only be read once.");
            }
            _started = true;

            IDataMessage? message;

            do
            {
                message = await _observer.TryReadNextAsync(cancellationToken);

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
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _observer.Dispose();
        }
    }
}
