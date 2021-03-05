using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace StrawberryShake
{
    internal class StoredOperation<T>
        : IStoredOperation
        , IObservable<IOperationResult<T>>
        where T : class
    {
        private readonly object _sync = new();
        private ImmutableList<Subscription> _subscriptions = ImmutableList<Subscription>.Empty;
        private bool _disposed;

        public StoredOperation(OperationRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public OperationRequest Request { get; }

        public IOperationResult<T>? LastResult { get; private set; }

        public IReadOnlyCollection<EntityId> EntityIds =>
            LastResult?.DataInfo?.EntityIds ??
            Array.Empty<EntityId>();

        public ulong Version => LastResult?.DataInfo?.Version ?? 0;

        public void SetResult(
            IOperationResult<T> result)
        {
            LastResult = result ?? throw new ArgumentNullException(nameof(result));

            // capture current subscriber list
            ImmutableList<Subscription> observers = _subscriptions;

            if (observers.IsEmpty)
            {
                // if there are now subscribers we will just return and waste no time.
                return;
            }

            // if we have subscribers we will invoke every one of them
            foreach (Subscription observer in observers)
            {
                observer.OnNext(result);
            }
        }

        public void ClearResult()
        {
            LastResult = null;
        }

        public void UpdateResult(ulong version)
        {
            if (LastResult is { DataInfo: not null } result)
            {
                SetResult(
                    result.WithData(
                        result.DataFactory.Create(result.DataInfo),
                        result.DataInfo.WithVersion(version)));
            }
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
                ImmutableList<Subscription> subscriptions = _subscriptions;

                foreach (Subscription subscription in subscriptions)
                {
                    subscription.Dispose();
                }

                _disposed = true;
            }
        }

        private class Subscription : IDisposable
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
}
