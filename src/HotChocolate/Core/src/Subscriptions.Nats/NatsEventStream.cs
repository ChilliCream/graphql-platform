using AlterNats;
using HotChocolate.Execution;

namespace HotChocolate.Subscriptions.Nats;

public class NatsEventStream<TMessage> : ISourceStream<TMessage>
{
    private readonly IObservable<TMessage> _observable;

    public NatsEventStream(string topic, NatsConnection connection)
    {
        _observable = connection.AsObservable<TMessage>(topic);
    }

    /// <inheritdoc />
    IAsyncEnumerable<TMessage> ISourceStream<TMessage>.ReadEventsAsync() =>
        new NatsAsyncEnumerable<TMessage>(_observable);

    /// <inheritdoc />
    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync() =>
        new NatsAsyncEnumerable<object>((IObservable<object>)_observable);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private class NatsAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IObservable<T> _observable;

        public NatsAsyncEnumerable(IObservable<T> observable)
        {
            _observable = observable;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            return new NatsAsyncEnumerator<T>(_observable, cancellationToken);
        }
    }

    private class NatsAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IAsyncEnumerator<T> _enumerator;

        public NatsAsyncEnumerator(IObservable<T> observable, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _enumerator = observable.ToAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
        }

        public T Current => _enumerator.Current;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            var hasNext = await _enumerator.MoveNextAsync().ConfigureAwait(false);
            if (hasNext)
            {
                if (Current != null && Current.Equals(NatsPubSub.Completed))
                {
                    return false;
                }
            }

            return hasNext;
        }

        public ValueTask DisposeAsync()
        {
            return _enumerator.DisposeAsync();
        }
    }
}
