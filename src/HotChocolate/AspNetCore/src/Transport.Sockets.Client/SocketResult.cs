using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Transport.Sockets.Client;

/// <summary>
/// Represents the result of a WebSocket operation that returns a stream of data.
/// </summary>
public sealed class SocketResult : IDisposable
{
    private readonly ResultEnumerable _enumerable;
    private bool _disposed;

    internal SocketResult(
        DataMessageObserver observer,
        IDisposable subscription,
        IDataCompletion completion)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        if (subscription is null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        _enumerable = new ResultEnumerable(observer, subscription, completion);
    }

    /// <summary>
    /// Returns an asynchronous stream of <see cref="OperationResult"/> objects
    /// representing the data returned by the WebSocket operation.
    /// </summary>
    /// <returns>An asynchronous stream of <see cref="OperationResult"/> objects.</returns>
    public IAsyncEnumerable<OperationResult> ReadResultsAsync() => _enumerable;

    /// <summary>
    /// Releases the resources used by this <see cref="SocketResult"/> object.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _enumerable.Dispose();
            _disposed = true;
        }
    }

    private sealed class ResultEnumerable(
        DataMessageObserver observer,
        IDisposable subscription,
        IDataCompletion completion)
        : IAsyncEnumerable<OperationResult>, IDisposable
    {
        private bool _started;

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
                message = await observer.TryReadNextAsync(cancellationToken);

                switch (message)
                {
                    case NextMessage next:
                        yield return next.Payload;
                        break;

                    case ErrorMessage error:
                        yield return error.Payload;
                        message = null;
                        completion.MarkDataStreamCompleted();
                        break;

                    case CompleteMessage:
                        message = null;
                        completion.MarkDataStreamCompleted();
                        break;
                }
            } while (!cancellationToken.IsCancellationRequested && message is not null);

            completion.TrySendCompleteMessage();
        }

        public void Dispose()
        {
            completion.TrySendCompleteMessage();
            subscription.Dispose();
            observer.Dispose();
        }
    }
}
