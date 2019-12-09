using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public static class SubscribeResolverObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IObservable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IObservable<T> observable = await subscribe(ctx).ConfigureAwait(false);
                return new ObservableSourceStreamAdapter<T>(observable);
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IObservable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IEnumerable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IEnumerable<T> enumerable = await subscribe(ctx).ConfigureAwait(false);
                return new EnumerableSourceStreamAdapter<T>(enumerable);
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IEnumerable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IAsyncEnumerable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IAsyncEnumerable<T> enumerable = await subscribe(ctx).ConfigureAwait(false);
                return new AsyncEnumerableStreamAdapter<T>(enumerable);
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IAsyncEnumerable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        private sealed class ObservableSourceStreamAdapter<T>
            : IObserver<T>
            , IAsyncEnumerable<object>
        {
            private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
            private readonly IDisposable _subscription;
            private TaskCompletionSource<object> _wait;
            private Exception _exception;
            private bool _isCompleted;

            public ObservableSourceStreamAdapter(IObservable<T> observable)
            {
                _subscription = observable.Subscribe(this);
            }

            public async IAsyncEnumerator<object> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                try
                {
                    _wait = new TaskCompletionSource<object>();
                    _wait.TrySetCanceled(cancellationToken);

                    while (!cancellationToken.IsCancellationRequested
                        || !_isCompleted)
                    {
                        if (!_queue.TryDequeue(out T item))
                        {
                            yield return item;
                        }
                        else if (_wait.Task.IsCompleted)
                        {
                            _wait = new TaskCompletionSource<object>();
                            _wait.TrySetCanceled(cancellationToken);
                        }
                        else
                        {
                            await _wait.Task.ConfigureAwait(false);
                        }

                        if (_exception is { })
                        {
                            _isCompleted = true;
                            throw _exception;
                        }
                    }
                }
                finally
                {
                    _subscription.Dispose();
                }
            }

            public void OnCompleted()
            {
                _isCompleted = true;
                _wait.SetResult(null);
            }

            public void OnError(Exception error)
            {
                _exception = error;
                _wait.SetResult(null);
            }

            public void OnNext(T value)
            {
                _queue.Enqueue(value);

                if (_wait != null && !_wait.Task.IsCompleted)
                {
                    _wait.SetResult(null);
                }
            }
        }

        private sealed class EnumerableSourceStreamAdapter<T>
            : IAsyncEnumerable<object>
        {
            private readonly IEnumerable<T> _enumerable;

            public EnumerableSourceStreamAdapter(IEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public IAsyncEnumerator<object> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                return new Enumerator(_enumerable.GetEnumerator(), cancellationToken);
            }

            private sealed class Enumerator
                : IAsyncEnumerator<object>
            {
                private readonly IEnumerator<T> _enumerator;
                private readonly CancellationToken _cancellationToken;

                public Enumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken)
                {
                    _enumerator = enumerator;
                    _cancellationToken = cancellationToken;
                }

                public object Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        Current = null;
                        return new ValueTask<bool>(false);
                    }

                    bool result = _enumerator.MoveNext();
                    Current = result ? (object)_enumerator.Current : null;
                    return new ValueTask<bool>(result);
                }

                public ValueTask DisposeAsync() => default;
            }
        }

        private sealed class AsyncEnumerableStreamAdapter<T>
            : IAsyncEnumerable<object>
        {
            private readonly IAsyncEnumerable<T> _enumerable;

            public AsyncEnumerableStreamAdapter(IAsyncEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
            }

            public async IAsyncEnumerator<object> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                await foreach (T item in _enumerable.WithCancellation(cancellationToken))
                {
                    yield return item;
                }
            }
        }
    }
}
