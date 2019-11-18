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

        public async Task<IOperationResult> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var resultBuilder = OperationResultBuilder.New(operation.ResultType);

            await ExecuteOperationAsync(
                operation,
                resultBuilder,
                cancellationToken)
                .ConfigureAwait(false);

            return resultBuilder.Build();
        }

        public async Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
            where T : class
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            var resultBuilder = OperationResultBuilder.New<T>();

            await ExecuteOperationAsync(
                operation,
                resultBuilder,
                cancellationToken)
                .ConfigureAwait(false);

            return resultBuilder.Build();
        }

        private async Task ExecuteOperationAsync(
            IOperation operation,
            IOperationResultBuilder resultBuilder,
            CancellationToken cancellationToken)
        {
            var context = new HttpOperationContext(
                operation,
                _clientFactory(),
                _services,
                resultBuilder,
                cancellationToken);

            try
            {
                await _executeOperation(context).ConfigureAwait(false);
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
