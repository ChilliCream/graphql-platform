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
        private readonly Func<HttpClient> _clientFactory;
        private readonly OperationDelegate _executeOperation;
        private readonly IServiceProvider _services;

        public HttpOperationExecutor(
            Func<HttpClient> clientFactory,
            OperationDelegate executeOperation,
            IServiceProvider services)
        {
            _clientFactory = clientFactory
                ?? throw new ArgumentNullException(nameof(clientFactory));
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
            where T : class
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
           where T : class
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
                operation, _clientFactory(), _services, cancellationToken);

            try
            {
                await _executeOperation(context).ConfigureAwait(false);

                if (context.Result is null)
                {
                    // todo : resources
                    throw new InvalidOperationException();
                }

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
}
