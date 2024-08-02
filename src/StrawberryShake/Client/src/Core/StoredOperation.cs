using System.Collections.Immutable;

namespace StrawberryShake;

internal class StoredOperation<T>
    : IStoredOperation
    , IObservable<IOperationResult<T>>
    where T : class
{
    private readonly object _sync = new();
    private ImmutableList<Subscription> _subscriptions = ImmutableList<Subscription>.Empty;
    private bool _disposed;
    private IOperationResult<T>? _lastResult;

    public StoredOperation(OperationRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public OperationRequest Request { get; }

    public IOperationResult<T>? LastResult
    {
        get => _lastResult;
        private set => _lastResult = value;
    }

    IOperationResult? IStoredOperation.LastResult => LastResult;

    public IReadOnlyCollection<EntityId> EntityIds =>
        LastResult?.DataInfo?.EntityIds ??
        Array.Empty<EntityId>();

    public ulong Version => LastResult?.DataInfo?.Version ?? 0;

    public int Subscribers => _subscriptions.Count;

    public DateTime LastModified { get; private set; } = DateTime.UtcNow;

    public void SetResult(
        IOperationResult<T> result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var updated = LastResult is null or { Data: null, } ||
            result.Data is null ||
            !result.Data.Equals(LastResult?.Data);
        LastResult = result;
        LastModified = DateTime.UtcNow;

        // capture current subscriber list
        var observers = _subscriptions;

        if (!updated || observers.IsEmpty)
        {
            // if there are now subscribers we will just return and waste no time.
            return;
        }

        // if we have subscribers we will invoke every one of them
        foreach (var observer in observers)
        {
            observer.OnNext(result);
        }
    }

    public void ClearResult()
    {
        LastResult = null;
        LastModified = DateTime.UtcNow;
    }

    public void UpdateResult(ulong version)
    {
        if (LastResult is { DataInfo: { } dataInfo, } result)
        {
            SetResult(
                result.WithData(
                    result.DataFactory.Create(dataInfo),
                    dataInfo.WithVersion(version)));
        }
    }

    public void Complete()
    {
        // capture current subscriber list
        var observers = _subscriptions;

        // if we have subscribers we will dispose every one of them
        foreach (var observer in observers)
        {
            observer.Dispose();
        }

        // clear subscribers
        _subscriptions = ImmutableList<Subscription>.Empty;
    }

    public IDisposable Subscribe(
        IObserver<IOperationResult<T>> observer)
    {
        if (observer is null)
        {
            throw new ArgumentNullException(nameof(observer));
        }

        var subscription = new Subscription(observer, Unsubscribe);

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

    public void Dispose()
    {
        if (!_disposed)
        {
            // capture current subscriber list
            var subscriptions = _subscriptions;

            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            _disposed = true;
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly IObserver<IOperationResult<T>> _observer;
        private readonly Action<Subscription> _unsubscribe;
        private bool _dispose;

        public Subscription(
            IObserver<IOperationResult<T>> observer,
            Action<Subscription> unsubscribe)
        {
            _observer = observer;
            _unsubscribe = unsubscribe;
        }

        public void OnNext(IOperationResult<T> result) =>
            _observer.OnNext(result);

        public void Dispose()
        {
            if (!_dispose)
            {
                _unsubscribe(this);
                _observer.OnCompleted();
                _dispose = true;
            }
        }
    }
}
