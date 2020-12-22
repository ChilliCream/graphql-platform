using System;
using System.Threading;
using System.Threading.Tasks;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake.Http
{
    public class OperationExecutor<TData, TResult>
        : IOperationExecutor<TResult>
        where TResult : class
        where TData : class
    {
        private readonly IConnection<TData> _connection;
        private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
        private readonly IOperationStore _operationStore;
        private readonly ExecutionStrategy _strategy;

        public OperationExecutor(
            IConnection<TData> connection,
            Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
            IOperationStore operationStore,
            ExecutionStrategy strategy = ExecutionStrategy.NetworkOnly)
        {
            _connection = connection;
            _resultBuilder = resultBuilder;
            _operationStore = operationStore;
            _strategy = strategy;
        }

        public async Task<IOperationResult<TResult>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IOperationResult<TResult>? result = null;
            IOperationResultBuilder<TData, TResult> resultBuilder = _resultBuilder();

            await foreach (var response in _connection.ExecuteAsync(request, cancellationToken))
            {
                result = resultBuilder.Build(response);
            }

            if (result is null)
            {
                throw new InvalidOperationException(HttpOperationExecutor_ExecuteAsync_NoResult);
            }

            return result;
        }

        public IOperationObservable<TResult> Watch(
            OperationRequest request,
            ExecutionStrategy? strategy = null)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new HttpOperationExecutorObservable(
                _connection,
                _operationStore,
                _resultBuilder,
                request,
                strategy ?? _strategy);
        }

        private class HttpOperationExecutorObservable : IOperationObservable<TResult>
        {
            private readonly IConnection<TData> _connection;
            private readonly IOperationStore _operationStore;
            private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
            private readonly OperationRequest _request;
            private readonly ExecutionStrategy _strategy;

            public HttpOperationExecutorObservable(
                IConnection<TData> connection,
                IOperationStore operationStore,
                Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
                OperationRequest request,
                ExecutionStrategy strategy)
            {
                _connection = connection;
                _operationStore = operationStore;
                _resultBuilder = resultBuilder;
                _request = request;
                _strategy = strategy;
            }

            public IDisposable Subscribe(
                IObserver<IOperationResult<TResult>> observer)
            {
                var hasResultInStore = false;

                if ((_strategy == ExecutionStrategy.CacheFirst ||
                    _strategy == ExecutionStrategy.CacheAndNetwork) &&
                    _operationStore.TryGet(_request, out IOperationResult<TResult>? result))
                {
                    hasResultInStore = true;
                    observer.OnNext(result);
                }

                IDisposable session = _operationStore.Watch<TResult>(_request).Subscribe(observer);

                if (_strategy != ExecutionStrategy.CacheFirst || !hasResultInStore)
                {
                    BeginExecute();
                }

                return session;
            }

            public async ValueTask<IAsyncDisposable> SubscribeAsync(
                IAsyncObserver<IOperationResult<TResult>> observer,
                CancellationToken cancellationToken = default)
            {
                var hasResultInStore = false;

                if ((_strategy == ExecutionStrategy.CacheFirst ||
                     _strategy == ExecutionStrategy.CacheAndNetwork) &&
                    _operationStore.TryGet(_request, out IOperationResult<TResult>? result))
                {
                    hasResultInStore = true;
                    await observer
                        .OnNextAsync(result, cancellationToken)
                        .ConfigureAwait(false);
                }

                IAsyncDisposable session = await _operationStore
                    .Watch<TResult>(_request)
                    .SubscribeAsync(observer, cancellationToken)
                    .ConfigureAwait(false);

                if (_strategy != ExecutionStrategy.CacheFirst || !hasResultInStore)
                {
                    BeginExecute(cancellationToken);
                }

                return session;
            }

            public void Subscribe(
                Action<IOperationResult<TResult>> next,
                CancellationToken cancellationToken = default)
            {
                IDisposable session = Subscribe(new DelegateObserver<TResult>(next));
                cancellationToken.Register(() => session.Dispose());
            }

            public void Subscribe(
                Func<IOperationResult<TResult>, CancellationToken, ValueTask> nextAsync,
                CancellationToken cancellationToken = default)
            {
                Task.Run(async () =>
                {
                    IAsyncDisposable session =
                        await SubscribeAsync(
                            new AsyncDelegateObserver<TResult>(nextAsync),
                            cancellationToken)
                            .ConfigureAwait(false);

                    cancellationToken.Register(() => session.DisposeAsync());
                }, cancellationToken);
            }


            private void BeginExecute(CancellationToken cancellationToken = default) =>
                Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);

            private async Task ExecuteAsync(
                CancellationToken cancellationToken)
            {
                IOperationResultBuilder<TData, TResult> resultBuilder = _resultBuilder();

                await foreach (var response in
                    _connection.ExecuteAsync(_request, cancellationToken).ConfigureAwait(false))
                {
                    resultBuilder.Build(response);
                }
            }
        }
    }
}
