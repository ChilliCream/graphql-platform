#if FUSION
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
using ResultDocument = HotChocolate.Fusion.Text.Json.SourceResultDocument;
#else
using HotChocolate.Transport.Sockets.Client.Protocols;
using HotChocolate.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
using ResultDocument = HotChocolate.Transport.OperationResult;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

/// <summary>
/// Represents the result of a WebSocket operation that returns a stream of data.
/// </summary>
public sealed class SocketResult : IDisposable
#if FUSION
    , IAsyncDisposable
#endif
{
    private readonly ResultEnumerable _enumerable;
    private bool _disposed;

    internal SocketResult(
        DataMessageObserver observer,
        IDisposable subscription,
        IDataCompletion completion,
        CancellationTokenRegistration cancellationRegistration)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(subscription);

        _enumerable = new ResultEnumerable(observer, subscription, completion, cancellationRegistration);
    }

    /// <summary>
    /// Returns an asynchronous stream of result documents representing the data returned by the WebSocket operation.
    /// </summary>
    /// <returns>An asynchronous stream of result documents.</returns>
    public IAsyncEnumerable<ResultDocument> ReadResultsAsync() => _enumerable;

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

#if FUSION
    /// <summary>
    /// Completes the operation and waits until its complete message has been sent.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for waiting for completion.</param>
    public async ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
        await _enumerable.CompleteAsync(cancellationToken);
        _disposed = true;
    }

    /// <summary>
    /// Asynchronously releases the resources used by this <see cref="SocketResult"/> object.
    /// </summary>
    public ValueTask DisposeAsync()
        => CompleteAsync(default);
#endif

    private sealed class ResultEnumerable(
        DataMessageObserver observer,
        IDisposable subscription,
        IDataCompletion completion,
        CancellationTokenRegistration cancellationRegistration)
        : IAsyncEnumerable<ResultDocument>, IDisposable
    {
        private bool _started;
        private int _disposed;

        public async IAsyncEnumerator<ResultDocument> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            if (_started)
            {
                throw new InvalidOperationException("This stream can only be read once.");
            }
            _started = true;

            IDataMessage? message;

            try
            {
                do
                {
                    message = await observer.TryReadNextAsync(cancellationToken);

                    switch (message)
                    {
                        case NextMessage next:
#if FUSION
                            try
                            {
                                yield return next.TakePayload();
                            }
                            finally
                            {
                                next.Dispose();
                            }
#else
                            yield return next.Payload;
#endif
                            break;

                        case ErrorMessage error:
#if FUSION
                            try
                            {
                                yield return error.TakePayload();
                            }
                            finally
                            {
                                error.Dispose();
                            }
#else
                            yield return error.Payload;
#endif
                            message = null;
                            completion.MarkDataStreamCompleted();
                            break;

                        case CompleteMessage:
                            message = null;
                            completion.MarkDataStreamCompleted();
                            break;
                    }
                } while (!cancellationToken.IsCancellationRequested && message is not null);
            }
            finally
            {
#if FUSION
                _ = completion.TrySendCompleteMessageAsync();
#else
                completion.TrySendCompleteMessage();
#endif
            }
        }

#if FUSION
        public async ValueTask CompleteAsync(CancellationToken cancellationToken)
        {
            await completion.TrySendCompleteMessageAsync().AsTask().WaitAsync(cancellationToken);
            Dispose();
        }
#endif

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

#if FUSION
            _ = completion.TrySendCompleteMessageAsync();
#else
            completion.TrySendCompleteMessage();
#endif
            cancellationRegistration.Dispose();
            subscription.Dispose();
            observer.Dispose();
        }
    }
}
