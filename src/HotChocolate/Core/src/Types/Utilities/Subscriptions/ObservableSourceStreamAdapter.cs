using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Utilities.Subscriptions
{
    internal sealed class ObservableSourceStreamAdapter<T>
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
                cancellationToken.Register(() => _wait.TrySetCanceled());

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_queue.TryDequeue(out T item))
                    {
                        yield return item;
                    }
                    else if (_isCompleted)
                    {
                        break;
                    }
                    else if (_wait.Task.IsCompleted)
                    {
                        _wait = new TaskCompletionSource<object>();
                    }
                    else if (_queue.Count == 0)
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

            if (_wait != null && !_wait.Task.IsCompleted)
            {
                _wait.SetResult(null);
            }
        }

        public void OnError(Exception error)
        {
            _exception = error;

            if (_wait != null && !_wait.Task.IsCompleted)
            {
                _wait.SetResult(null);
            }
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
}
