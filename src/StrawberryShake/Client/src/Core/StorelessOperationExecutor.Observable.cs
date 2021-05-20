using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public partial class StorelessOperationExecutor<TData, TResult>
    {
        private class StorelessOperationExecutorObservable : IObservable<IOperationResult<TResult>>
        {
            private readonly IConnection<TData> _connection;
            private readonly Func<IOperationResultBuilder<TData, TResult>> _resultBuilder;
            private readonly OperationRequest _request;

            public StorelessOperationExecutorObservable(
                IConnection<TData> connection,
                Func<IOperationResultBuilder<TData, TResult>> resultBuilder,
                OperationRequest request)
            {
                _connection = connection;
                _resultBuilder = resultBuilder;
                _request = request;
            }

            public IDisposable Subscribe(IObserver<IOperationResult<TResult>> observer)
            {
                var observerSession = new ObserverSession();
                BeginExecute(observer, observerSession);
                return observerSession;
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

                        observer.OnNext(resultBuilder.Build(response));
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
