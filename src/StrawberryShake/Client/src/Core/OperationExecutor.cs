using System;
using System.Threading;
using System.Threading.Tasks;
using static StrawberryShake.Properties.Resources;

namespace StrawberryShake
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
            _connection = connection ??
                throw new ArgumentNullException(nameof(connection));
            _resultBuilder = resultBuilder ??
                throw new ArgumentNullException(nameof(resultBuilder));
            _operationStore = operationStore ??
                throw new ArgumentNullException(nameof(operationStore));
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

        public IObservable<IOperationResult<TResult>> Watch(
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

        private class HttpOperationExecutorObservable : IObservable<IOperationResult<TResult>>
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

                IDisposable session =
                    _operationStore.Watch<TResult>(_request).Subscribe(observer);

                if (_request.Document.Kind == OperationKind.Subscription ||
                    _strategy != ExecutionStrategy.CacheFirst ||
                    !hasResultInStore)
                {
                    var requestSession = new RequestSession();
                    BeginExecute(requestSession);
                    return new ObserverSession(session, requestSession);
                }

                return session;
            }

            private void BeginExecute(RequestSession session) =>
                Task.Run(() => ExecuteAsync(session));

            private async Task ExecuteAsync(RequestSession session)
            {
                try
                {
                    IOperationResultBuilder<TData, TResult> resultBuilder = _resultBuilder();

                    await foreach (var response in
                        _connection.ExecuteAsync(_request, session.Token).ConfigureAwait(false))
                    {
                        if (session.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        _operationStore.Set(_request, resultBuilder.Build(response));
                    }
                }
                finally
                {
                    session.Dispose();
                }
            }

            private class ObserverSession : IDisposable
            {
                private readonly IDisposable _storeSession;
                private readonly RequestSession _requestSession;
                private bool _disposed;

                public ObserverSession(IDisposable storeSession, RequestSession requestSession)
                {
                    _storeSession = storeSession;
                    _requestSession = requestSession;
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        _requestSession.Dispose();
                        _storeSession.Dispose();
                        _disposed = true;
                    }
                }
            }

            private class RequestSession : IDisposable
            {
                private readonly CancellationTokenSource _cts;
                private bool _disposed;

                public RequestSession()
                {
                    _cts = new CancellationTokenSource();
                }

                public CancellationToken Token => _cts.Token;

                public void Cancel()
                {
                    try
                    {
                        if (!_disposed)
                        {
                            _cts.Cancel();
                        }
                    }
                    catch(ObjectDisposedException)
                    {
                        // we do not care if this happens.
                    }
                }

                public void Dispose()
                {
                    if (!_disposed)
                    {
                        Cancel();
                        _cts.Dispose();
                        _disposed = true;
                    }
                }
            }
        }
    }
}
