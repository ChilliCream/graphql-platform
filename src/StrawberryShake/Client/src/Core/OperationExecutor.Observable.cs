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

            public IDisposable Subscribe(IObserver<IOperationResult<TResult>> observer)
            {
                if (_strategy == ExecutionStrategy.NetworkOnly ||
                    _request.Document.Kind == OperationKind.Subscription)
                {
                    var observerSession = new ObserverSession();
                    BeginExecute(observer, observerSession);
                    return observerSession;
                }

                var hasResultInStore = false;

                if ((_strategy == ExecutionStrategy.CacheFirst ||
                     _strategy == ExecutionStrategy.CacheAndNetwork) &&
                    _operationStore.TryGet(_request, out IOperationResult<TResult>? result))
                {
                    hasResultInStore = true;
                    observer.OnNext(result);
                }

                IDisposable session = _operationStore.Watch<TResult>(_request).Subscribe(observer);

                if (_strategy != ExecutionStrategy.CacheFirst ||
                    !hasResultInStore)
                {
                    var observerSession = new ObserverSession();
                    observerSession.SetStoreSession(session);
                    BeginExecute(observer, observerSession);
                    return observerSession;
                }

                return session;
            }

            private void BeginExecute(
                IObserver<IOperationResult<TResult>> observer,
                ObserverSession session) =>
                Task.Run(() => ExecuteAsync(observer, session));

            private async Task ExecuteAsync(
                IObserver<IOperationResult<TResult>> observer,
                ObserverSession session)
            {
                try
                {
                    CancellationToken token = session.RequestSession.Token;
                    IOperationResultBuilder<TData, TResult> resultBuilder = _resultBuilder();

                    await foreach (var response in
                        _connection.ExecuteAsync(_request, token).ConfigureAwait(false))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        IOperationResult<TResult> result = resultBuilder.Build(response);
                        _operationStore.Set(_request, result);

                        if (!session.HasStoreSession)
                        {
                            observer.OnNext(result);

                            IDisposable storeSession =
                                _operationStore
                                    .Watch<TResult>(_request)
                                    .Subscribe(observer);

                            try
                            {
                                session.SetStoreSession(storeSession);
                            }
                            catch (ObjectDisposedException)
                            {
                                storeSession.Dispose();
                                throw;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
                finally
                {
                    // after all the transport logic is finished we will dispose
                    // the request session.
                    session.RequestSession.Dispose();
                }
            }
        }
    }
}
