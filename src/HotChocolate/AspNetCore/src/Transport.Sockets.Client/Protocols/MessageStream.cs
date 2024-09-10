using System.Collections.Immutable;

namespace HotChocolate.Transport.Sockets.Client.Protocols;

internal sealed class MessageStream : IObservable<IOperationMessage>, IObserver<IOperationMessage>
{
    private readonly object _sync = new();
    private ImmutableList<Subscription> _subscriptions = ImmutableList<Subscription>.Empty;

    public IDisposable Subscribe(IObserver<IOperationMessage> observer)
    {
        var subscription = new Subscription(observer);
        subscription.Disposed += (_, _) => Unsubscribe(subscription);

        lock (_sync)
        {
            _subscriptions = _subscriptions.Add(subscription);
        }

        return subscription;
    }

    private void Unsubscribe(Subscription subscription)
    {
        lock (_sync)
        {
            _subscriptions = _subscriptions.Remove(subscription);
        }
    }

    public void OnNext(IOperationMessage value) => OnNext(value, _subscriptions);

    private void OnNext(IOperationMessage value, ImmutableList<Subscription> subscriptions)
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Observer.OnNext(value);
        }
    }

    public void OnError(Exception error) => OnError(error, _subscriptions);

    private void OnError(Exception error, ImmutableList<Subscription> subscriptions)
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Observer.OnError(error);
        }
    }

    public void OnCompleted() => OnCompleted(_subscriptions);

    private void OnCompleted(ImmutableList<Subscription> subscriptions)
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Observer.OnCompleted();
        }
    }

    private sealed class Subscription(IObserver<IOperationMessage> observer) : IDisposable
    {
        private bool _disposed;

        public event EventHandler? Disposed;

        public IObserver<IOperationMessage> Observer { get; } = observer;

        public void Dispose()
        {
            if (!_disposed)
            {
                Disposed?.Invoke(this, EventArgs.Empty);
                _disposed = true;
            }
        }
    }
}
