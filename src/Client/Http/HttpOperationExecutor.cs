using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutor
        : IOperationExecutor
    {
        private readonly HttpClient _client;
        private readonly OperationDelegate _executeOperation;
        private readonly IServiceProvider _services;

        public HttpOperationExecutor(
            HttpClient client,
            OperationDelegate executeOperation,
            IServiceProvider services)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client));
            _executeOperation = executeOperation
                ?? throw new ArgumentNullException(nameof(executeOperation));
            _services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public Task<IOperationResult> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return ExecuteOperationAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return ExecuteAndCastAsync<T>(operation, cancellationToken);
        }

        public async Task<IOperationResult<T>> ExecuteAndCastAsync<T>(
           IOperation<T> operation,
           CancellationToken cancellationToken)
        {
            IOperationResult result =
                await ExecuteOperationAsync(operation, cancellationToken)
                    .ConfigureAwait(false);
            return (IOperationResult<T>)result;
        }

        private async Task<IOperationResult> ExecuteOperationAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            var context = new HttpOperationContext(
                operation, _client, _services, cancellationToken);

            try
            {
                await _executeOperation(context).ConfigureAwait(false);
                return context.Result;
            }
            finally
            {
                if (context.HttpResponse != null)
                {
                    context.HttpResponse.Dispose();
                }

                if (context.HttpRequest != null)
                {
                    context.HttpRequest.Dispose();
                }
            }
        }
    }

    public class HttpOperationExecutorBuilder
    {
        public HttpOperationExecutorBuilder SetClient()
        {

        }

        public HttpOperationExecutorBuilder SetServices(IServiceProvider services)
        {

        }

        public HttpOperationExecutorBuilder Use(Func<OperationDelegate, OperationDelegate> middleware);

        HttpOperationExecutorBuilder Use(OperationMiddleware middleware);

    }
}
