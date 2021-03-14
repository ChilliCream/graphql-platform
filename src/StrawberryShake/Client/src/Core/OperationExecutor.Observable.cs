using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public partial class OperationExecutor<TData, TResult>
    {
        private class OperationExecutorObservable : IObservable<IOperationResult<TResult>>
        {
            private readonly IConnection<TData> _connection;
            private readonly IOperationStore _operationStore;
            private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
            private readonly OperationRequest _request;
            private readonly ExecutionStrategy _strategy;

            public OperationExecutorObservable(
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

                        if (_request.Document.Kind == OperationKind.Subscription)
                        {
                            _operationStore.Reset(_request);
                        }
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
