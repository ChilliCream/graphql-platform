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
        private readonly HttpOperationDelegate _executeOperation;
        private readonly IResultParserCollection _resultParserResolver;

        public HttpOperationExecutor(
            Func<HttpClient> clientFactory,
            HttpOperationDelegate executeOperation,
            IResultParserCollection resultParserResolver)
        {
            _clientFactory = clientFactory
                ?? throw new ArgumentNullException(nameof(clientFactory));
            _executeOperation = executeOperation
                ?? throw new ArgumentNullException(nameof(executeOperation));
            _resultParserResolver = resultParserResolver
                ?? throw new ArgumentNullException(nameof(resultParserResolver));
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
                resultBuilder,
                _resultParserResolver.GetByResult(operation.ResultType),
                _clientFactory(),
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
