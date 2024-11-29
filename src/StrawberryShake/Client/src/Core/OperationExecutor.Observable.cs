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
                return BeginExecute(observer, lastEmittedResult: null);
            }

            if (_operationStore.TryGet(_request, out IOperationResult<TResult>? result))
            {
                observer.OnNext(result);

                if (_strategy is ExecutionStrategy.CacheFirst)
                {
                    // Skip the network request and just subscribe to store updates
                    return _operationStore.Watch<TResult>(_request).Subscribe(observer);
                }
            }

            return BeginExecute(observer, lastEmittedResult: result);
        }

        private IDisposable BeginExecute(
            IObserver<IOperationResult<TResult>> observer,
            IOperationResult<TResult>? lastEmittedResult)
        {
            var observerSession = new ObserverSession();
            Task.Run(() => ExecuteAsync(observer, observerSession, lastEmittedResult));
            return observerSession;
        }

        private async Task ExecuteAsync(
            IObserver<IOperationResult<TResult>> observer,
            ObserverSession session,
            IOperationResult<TResult>? lastEmittedResult)
        {
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

                    IOperationResult<TResult>? result;
                    if (response.IsPatch)
                    {
                        var patched = resultPatcher.PatchResponse(response);
                        result = resultBuilder.Build(patched);
                    }
                    else
                    {
                        resultPatcher.SetResponse(response);
                        result = resultBuilder.Build(response);
                    }

                    // Emit the result, if it isn't a duplicate
                    if (!Equals(result.Data, lastEmittedResult?.Data))
                    {
                        observer.OnNext(result);
                        lastEmittedResult = result;
                    }
                    _operationStore.Set(_request, result);
                }

                if (_request.Document.Kind is OperationKind.Subscription)
                {
                    // If the subscription is completed by the server, notify observers.
                    // This should not be done for queries, as they should continue listening
                    // to updates from the entity store.
                    observer.OnCompleted();
                }
                else
                {
                    // Subscribe to updates from the entity store after the query has completed
                    TrySubscribeObserverSessionToStore(observer, session);
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
