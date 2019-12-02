using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class Subscription<T>
        : Subscription
        , IResponseStream<T> where T : class
    {
        private readonly SemaphoreSlim _initSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _resultSemaphore = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<IOperationResult<T>?>? _nextResult;
        private Func<Task>? _unregister;
        private bool _disposed;

        public Subscription(IOperation operation, IResultParser resultParser)
        {
            Id = Guid.NewGuid().ToString("N");
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            ResultParser = resultParser ?? throw new ArgumentNullException(nameof(resultParser));
        }

        public override string Id { get; }

        public override IOperation Operation { get; }

        public override IResultParser ResultParser { get; }

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

            while (!_disposed && !completed)
            {
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
                        _nextResult = new TaskCompletionSource<IOperationResult<T>?>(
                            cancellationToken);
                        _resultSemaphore.Release();
                    }
                }
            }
        }


        protected override IAsyncEnumerator<IOperationResult> OnGetAsyncEnumerator(
            CancellationToken cancellationToken) =>
            GetAsyncEnumerator(cancellationToken);


        public override void OnRegister(Func<Task> unregister)
        {
            _unregister = unregister ?? throw new ArgumentNullException(nameof(unregister));
        }

        public override Task OnReceiveResultAsync(
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

        public override async Task OnCompletedAsync(CancellationToken cancellationToken)
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

            await DisposeAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _initSemaphore.Dispose();
                _resultSemaphore.Dispose();

                if (_unregister is { })
                {
                    await _unregister().ConfigureAwait(false);
                    _unregister = null;
                }

                _disposed = true;
            }
        }
    }
}
