using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake;

public partial class OperationExecutor<TData, TResult>
{
    private sealed class OperationExecutorObservable : IObservable<IOperationResult<TResult>>
    {
        private readonly IConnection<TData> _connection;
        private readonly IOperationStore _operationStore;
        private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
        private readonly Func<IResultPatcher<TData>> _resultPatcher;
        private readonly OperationRequest _request;
        private readonly ExecutionStrategy _strategy;

        public OperationExecutorObservable(
            IConnection<TData> connection,
            IOperationStore operationStore,
            Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
            Func<IResultPatcher<TData>> resultPatcher,
            OperationRequest request,
            ExecutionStrategy strategy)
        {
            _connection = connection;
            _operationStore = operationStore;
            _resultBuilder = resultBuilder;
            _resultPatcher = resultPatcher;
            _request = request;
            _strategy = strategy;
        }

        public IDisposable Subscribe(IObserver<IOperationResult<TResult>> observer)
        {
            if (_strategy is ExecutionStrategy.NetworkOnly ||
                _request.Document.Kind is OperationKind.Subscription)
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

            if (_strategy is not ExecutionStrategy.CacheFirst || !hasResultInStore)
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
                IResultPatcher<TData> resultPatcher = _resultPatcher();

                await foreach (Response<TData>? response in
                    _connection.ExecuteAsync(_request)
                        .WithCancellation(token)
                        .ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    IOperationResult<TResult>? result;
                    if (response.IsPatch)
                    {
                        Response<TData> patched = resultPatcher.PatchResponse(response);
                        result = resultBuilder.Build(patched);
                    }
                    else
                    {
                        resultPatcher.SetResponse(response);
                        result = resultBuilder.Build(response);
                        _operationStore.Set(_request, result);
                    }

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
                // call observer's OnCompleted method to notify observer
                // there is no further data is available.
                observer.OnCompleted();
                
                // after all the transport logic is finished we will dispose
                // the request session.
                session.RequestSession.Dispose();
            }
        }
    }
}
