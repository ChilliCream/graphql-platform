using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Properties;
using StrawberryShake.Remove;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutor<T> : IOperationExecutor<T> where T : class
    {
        private readonly IConnection<JsonDocument> _connection;
        private readonly IOperationResultBuilder<JsonDocument, T> _resultBuilder;
        private readonly IOperationStore _operationStore;
        private readonly ExecutionStrategy _strategy;

        public async Task<IOperationResult<T>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IOperationResult<T>? result = null;

            await foreach (var response in _connection.ExecuteAsync(request, cancellationToken))
            {
                result = _resultBuilder.Build(response);
            }

            if (result is null)
            {
                throw new InvalidOperationException(HttpOperationExecutor_ExecuteAsync_NoResult);
            }

            return result;
        }

        public IOperationObservable<T> Watch(
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

        private class HttpOperationExecutorObservable : IOperationObservable<T>
        {
            private readonly IConnection<JsonDocument> _connection;
            private readonly IOperationStore _operationStore;
            private readonly IOperationResultBuilder<JsonDocument, T> _resultBuilder;
            private readonly OperationRequest _request;
            private readonly ExecutionStrategy _strategy;

            public HttpOperationExecutorObservable(
                IConnection<JsonDocument> connection,
                IOperationStore operationStore,
                IOperationResultBuilder<JsonDocument, T> resultBuilder,
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
                IObserver<IOperationResult<T>> observer)
            {
                var hasResultInStore = false;

                if ((_strategy == ExecutionStrategy.CacheFirst ||
                    _strategy == ExecutionStrategy.CacheAndNetwork) &&
                    _operationStore.TryGet(_request, out IOperationResult<T>? result))
                {
                    hasResultInStore = true;
                    observer.OnNext(result);
                }

                IDisposable session = _operationStore.Watch<T>(_request).Subscribe(observer);

                if (_strategy != ExecutionStrategy.CacheFirst || !hasResultInStore)
                {
                    BeginExecute();
                }

                return session;
            }

            public async ValueTask<IAsyncDisposable> SubscribeAsync(
                IAsyncObserver<IOperationResult<T>> observer,
                CancellationToken cancellationToken = default)
            {
                var hasResultInStore = false;

                if ((_strategy == ExecutionStrategy.CacheFirst ||
                     _strategy == ExecutionStrategy.CacheAndNetwork) &&
                    _operationStore.TryGet(_request, out IOperationResult<T>? result))
                {
                    hasResultInStore = true;
                    await observer
                        .OnNextAsync(result, cancellationToken)
                        .ConfigureAwait(false);
                }

                IAsyncDisposable session = await _operationStore
                    .Watch<T>(_request)
                    .SubscribeAsync(observer, cancellationToken)
                    .ConfigureAwait(false);

                if (_strategy != ExecutionStrategy.CacheFirst || !hasResultInStore)
                {
                    BeginExecute(cancellationToken);
                }

                return session;
            }

            public void Subscribe(
                Action<IOperationResult<T>> next,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(
                Func<IOperationResult<T>, CancellationToken, ValueTask> nextAsync,
                CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }


            private void BeginExecute(CancellationToken cancellationToken = default) =>
                Task.Run(() => ExecuteAsync(cancellationToken), cancellationToken);

            private async Task ExecuteAsync(
                CancellationToken cancellationToken)
            {
                await foreach (var response in
                    _connection.ExecuteAsync(_request, cancellationToken).ConfigureAwait(false))
                {
                    _resultBuilder.Build(response);
                }
            }
        }
    }

    public enum ExecutionStrategy
    {
        CacheFirst,
        CacheAndNetwork,
        NetworkOnly
    }
}
