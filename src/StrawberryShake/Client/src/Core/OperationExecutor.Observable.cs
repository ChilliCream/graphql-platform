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

            if (_strategy is not ExecutionStrategy.CacheFirst || !hasResultInStore)
            {
                var observerSession = new ObserverSession();
                BeginExecute(observer, observerSession);
                return observerSession;
            }

            return _operationStore.Watch<TResult>(_request).Subscribe(observer);
        }

        private void BeginExecute(
            IObserver<IOperationResult<TResult>> observer,
            ObserverSession session) =>
            Task.Run(() => ExecuteAsync(observer, session));

        private async Task ExecuteAsync(
            IObserver<IOperationResult<TResult>> observer,
            ObserverSession session)
        {
            TrySubscribeObserverSessionToStore(observer, session);

            try
            {
                var abort = session.RequestSession.Abort;
                var resultBuilder = _resultBuilder();
                var resultPatcher = _resultPatcher();

                await foreach (var response in
                    _connection.ExecuteAsync(_request)
                        .WithCancellation(abort)
                        .ConfigureAwait(false))
                {
                    if (abort.IsCancellationRequested)
                    {
                        return;
                    }

                    if (response.IsPatch)
                    {
                        var patched = resultPatcher.PatchResponse(response);
                        var result = resultBuilder.Build(patched);
                        _operationStore.Set(_request, result);
                    }
                    else
                    {
                        resultPatcher.SetResponse(response);
                        var result = resultBuilder.Build(response);
                        _operationStore.Set(_request, result);
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

        private void TrySubscribeObserverSessionToStore(
            IObserver<IOperationResult<TResult>> observer,
            ObserverSession session)
        {
            // we need to make sure that there is not already an store session associated
            // with the observer session.
            if (!session.HasStoreSession)
            {
                // next we subscribe to the store so that we get updates from the store as well
                // as from the data stream.
                var storeSession =
                    _operationStore
                        .Watch<TResult>(_request)
                        .Subscribe(observer);

                try
                {
                    session.SetStoreSession(storeSession);
                }
                catch (ObjectDisposedException)
                {
                    // if the user already unsubscribed we will get a dispose exception
                    // and will immediately dispose the store session.
                    storeSession.Dispose();
                    throw;
                }
            }
        }
    }
}
