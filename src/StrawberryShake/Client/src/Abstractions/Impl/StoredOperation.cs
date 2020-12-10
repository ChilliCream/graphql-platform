using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    internal class StoredOperation<T>
        : IStoredOperation
        , IOperationObservable<T> where T : class
    {
        private readonly object _sync = new();
        private ImmutableList<Subscription> _subscriptions = ImmutableList<Subscription>.Empty;
        private bool _disposed;

        public StoredOperation(IOperationRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public IOperationRequest Request { get; }

        public IOperationResult<T>? LastResult { get; private set; }

        public async ValueTask SetResultAsync(
            IOperationResult<T> result,
            CancellationToken cancellationToken = default)
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
                await observer.OnNextAsync(result, cancellationToken).ConfigureAwait(false);
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

        public ValueTask<IAsyncDisposable> SubscribeAsync(
            IAsyncObserver<IOperationResult<T>> observer,
            CancellationToken cancellationToken = default)
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

            return new ValueTask<IAsyncDisposable>(subscription);
        }

        public void Subscribe(
            Action<IOperationResult<T>> next,
            CancellationToken cancellationToken = default)
        {
            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var subscription = new Subscription(new DelegateObserver(next), Unsubscribe);
            cancellationToken.Register(() => subscription.Dispose());

            lock (_sync)
            {
                _subscriptions = _subscriptions.Add(subscription);
            }
        }

        public void Subscribe(
            Func<IOperationResult<T>, CancellationToken, ValueTask> nextAsync,
            CancellationToken cancellationToken = default)
        {
            if (nextAsync is null)
            {
                throw new ArgumentNullException(nameof(nextAsync));
            }

            var subscription = new Subscription(new AsyncDelegateObserver(nextAsync), Unsubscribe);
            cancellationToken.Register(() => subscription.Dispose());

            lock (_sync)
            {
                _subscriptions = _subscriptions.Add(subscription);
            }
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

        private class DelegateObserver : IObserver<IOperationResult<T>>
        {
            private readonly Action<IOperationResult<T>> _next;

            public DelegateObserver(Action<IOperationResult<T>> next)
            {
                _next = next;
            }

            public void OnNext(IOperationResult<T> value) => _next(value);

            public void OnError(Exception error)
            {
            }

            public void OnCompleted()
            {
            }
        }

        private class AsyncDelegateObserver : IAsyncObserver<IOperationResult<T>>
        {
            private readonly Func<IOperationResult<T>, CancellationToken, ValueTask> _nextAsync;

            public AsyncDelegateObserver(
                Func<IOperationResult<T>, CancellationToken, ValueTask> nextAsync)
            {
                _nextAsync = nextAsync;
            }

            public ValueTask OnNextAsync(
                IOperationResult<T> value,
                CancellationToken cancellationToken = default) =>
                _nextAsync(value, cancellationToken);

            public ValueTask OnErrorAsync(
                Exception error,
                CancellationToken cancellationToken = default) =>
                default;

            public ValueTask OnCompletedAsync() =>
                default;
        }

        private class Subscription : IDisposable, IAsyncDisposable
        {
            private readonly IAsyncObserver<IOperationResult<T>>? _asyncObserver;
            private readonly IObserver<IOperationResult<T>>? _observer;
            private readonly Action<Subscription> _unsubscribe;
            private bool _dispose;

            public Subscription(
                object observer,
                Action<Subscription> unsubscribe)
            {
                if (observer is IAsyncObserver<IOperationResult<T>> ao)
                {
                    _asyncObserver = ao;
                }
                else if (observer is IObserver<IOperationResult<T>> o)
                {
                    _observer = o;
                }

                _unsubscribe = unsubscribe;
            }

            public async ValueTask OnNextAsync(
                IOperationResult<T> result,
                CancellationToken cancellationToken = default)
            {
                if (_asyncObserver is not null)
                {
                    await _asyncObserver
                        .OnNextAsync(result, cancellationToken)
                        .ConfigureAwait(false);
                }

                _observer?.OnNext(result);
            }

            // we just invoke but do not await.
            public void Dispose() => DisposeAsync().ConfigureAwait(false);

            public async ValueTask DisposeAsync()
            {
                if (!_dispose)
                {
                    _unsubscribe(this);

                    if (_asyncObserver is not null)
                    {
                        await _asyncObserver
                            .OnCompletedAsync()
                            .ConfigureAwait(false);
                    }

                    _observer?.OnCompleted();

                    _dispose = true;
                }
            }
        }
    }
}
