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
        private readonly HttpConnection _connection;
        private readonly IOperationResultBuilder<JsonDocument, T> _resultBuilder;

        public async Task<IOperationResult<T>> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {
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

        public IOperationObservable<T> Watch(OperationRequest request)
        {
            throw new NotImplementedException();
        }

        private class GetFooQueryObservable : IOperationObservable<GetFooResult>
        {
            private readonly IOperationExecutor<GetFooResult> _operationExecutor;
            private readonly IOperationStore _operationStore;
            private readonly GetFooQueryRequest _request;

            public ValueTask<IAsyncDisposable> SubscribeAsync(IAsyncObserver<IOperationResult<GetFooResult>> observer, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(Func<IOperationResult<GetFooResult>, CancellationToken, ValueTask> nextAsync, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public IDisposable Subscribe(
                IObserver<IOperationResult<GetFooResult>> observer)
            {
                throw new NotImplementedException();
            }

            public void Subscribe(
                Action<IOperationResult<GetFooResult>> next,
                CancellationToken cancellationToken = default)
            {



                throw new NotImplementedException();
            }
            //
            // public void Subscribe(
            //     Func<IOperationResult<GetFooResult>, ValueTask> nextAsync,
            //     CancellationToken cancellationToken = default)
            // {
            //     Task.Run(async () =>
            //         {
            //             _operationStore
            //                 .Watch<GetFooResult>(_request)
            //                 .Subscribe(nextAsync, cancellationToken);
            //
            //             await _operationExecutor
            //                 .ExecuteAsync(_request, cancellationToken)
            //                 .ConfigureAwait(false);
            //         },
            //         cancellationToken);
            // }
        }

    }
}
