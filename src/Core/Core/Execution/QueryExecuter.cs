using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class QueryExecuter
        : IQueryExecuter
    {
        private readonly IServiceProvider _applicationServices;
        private readonly QueryDelegate _queryDelegate;

        public QueryExecuter(
            ISchema schema,
            IServiceProvider applicationServices,
            QueryDelegate queryDelegate)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _applicationServices = applicationServices
                ?? throw new ArgumentNullException(nameof(applicationServices));
            _queryDelegate = queryDelegate
                ?? throw new ArgumentNullException(nameof(queryDelegate));
        }

        public ISchema Schema { get; }

        public void Dispose()
        {
            if (_applicationServices is IDisposable d)
            {
                d.Dispose();
            }
        }

        public Task<IExecutionResult> ExecuteAsync(
            QueryRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ExecuteInternalAsync(request, cancellationToken);
        }

        private async Task<IExecutionResult> ExecuteInternalAsync(
            QueryRequest request,
            CancellationToken cancellationToken)
        {
            using (IServiceScope scope = _applicationServices.CreateScope())
            {

                IServiceProvider services =
                    (request.Services == null)
                        ? scope.ServiceProvider.Include(Schema.Services)
                        : scope.ServiceProvider.Include(request.Services);

                var context = new QueryContext(
                    Schema,
                    services,
                    request.ToReadOnly());

                return await ExecuteMiddlewareAsync(context)
                    .ConfigureAwait(false);
            }
        }

        private async Task<IExecutionResult> ExecuteMiddlewareAsync(
            IQueryContext context)
        {
            await _queryDelegate(context).ConfigureAwait(false);

            if (context.Result == null)
            {
                // TODO : Resources
                throw new InvalidOperationException();
            }

            if (context.Result is IQueryResult queryResult)
            {
                return queryResult.AsReadOnly();
            }

            return context.Result;
        }
    }
}
