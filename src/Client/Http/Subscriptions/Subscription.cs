using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class Subscription<T>
        : IResponseStream<T>
        , ISubscription
        where T : class
    {
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _resultSemaphore = new SemaphoreSlim(0, 1);
        private TaskCompletionSource<IOperationResult<T>?>? _nextResult;
        private bool _disposed;

        public event EventHandler? Disposed;

        public Subscription(IOperation operation, IResultParser resultParser)
            : this(Guid.NewGuid().ToString("N"), operation, resultParser)
        {
        }

        public Subscription(string id, IOperation operation, IResultParser resultParser)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id cannot be null or empty.", nameof(id));
            }

            if (operation is null)
            {
                throw new ArgumentNullException(nameof(resultParser));
            }

            if (resultParser is null)
            {
                throw new ArgumentNullException(nameof(resultParser));
            }

            Id = id;
            Operation = operation;
            ResultParser = resultParser;
        }

        public string Id { get; }

        public IOperation Operation { get; }

        public IResultParser ResultParser { get; }

        IAsyncEnumerator<IOperationResult> IAsyncEnumerable<IOperationResult>.GetAsyncEnumerator(
            CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);

        public async IAsyncEnumerator<IOperationResult<T>> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                yield break;
            }

            var completed = false;

            await InitializeResultTask(
                () => new TaskCompletionSource<IOperationResult<T>?>(cancellationToken),
                cancellationToken)
                .ConfigureAwait(false);

            try
            {
                IOperationResult<T>? result = await _nextResult!.Task.ConfigureAwait(false);
                if (result is { })
                {
                    yield return result;
                }
                else
                {
                    completed = true;
                }
            }
            finally
            {
                if (!completed)
                {
                    _nextResult = new TaskCompletionSource<IOperationResult<T>?>(cancellationToken);
                    _resultSemaphore.Release();
                }
            }
        }

        public Task OnReceiveResultAsync(
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return OnReceiveResultInternal(message, cancellationToken);
        }

        private async Task OnReceiveResultInternal(
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return;
            }

            await InitializeResultTask(
                () => new TaskCompletionSource<IOperationResult<T>?>(),
                cancellationToken)
                .ConfigureAwait(false);

            await _resultSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await InvokeSubscriptionMiddleware(message.Payload).ConfigureAwait(false);
                var result = (OperationResult<T>)message.Payload.Build();
                _nextResult!.SetResult(result);
            }
            catch (Exception ex)
            {
                _nextResult!.SetException(ex);
            }
        }

        private async Task InitializeResultTask(
            Func<TaskCompletionSource<IOperationResult<T>?>> factory,
            CancellationToken cancellationToken)
        {
            if (_nextResult is null)
            {
                await _initSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (_nextResult is null)
                {
                    _nextResult = factory();
                }

                _initSemaphore.Release();
            }
        }

        private Task InvokeSubscriptionMiddleware(IOperationResultBuilder builder)
        {
            return Task.CompletedTask;
        }

        public async Task OnCompletedAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return;
            }

            await InitializeResultTask(
                () => new TaskCompletionSource<IOperationResult<T>?>(),
                cancellationToken)
                .ConfigureAwait(false);
            _nextResult!.SetResult(null);

            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _initSemaphore.Dispose();
                _resultSemaphore.Dispose();
                _disposed = true;

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
