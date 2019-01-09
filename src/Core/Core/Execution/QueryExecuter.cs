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

            IRequestServiceScope serviceScope = CreateServiceScope(
                request.Services);

            var context = new QueryContext(
                Schema,
                serviceScope,
                request.ToReadOnly());

            return ExecuteMiddlewareAsync(context);
        }

        private async Task<IExecutionResult> ExecuteMiddlewareAsync(
            IQueryContext context)
        {
            try
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
            finally
            {
                if (!context.ServiceScope.IsLifetimeHandled)
                {
                    context.ServiceScope.Dispose();
                }
            }
        }

        private IRequestServiceScope CreateServiceScope(
            IServiceProvider requestServices)
        {
            IServiceScope serviceScope = _applicationServices.CreateScope();
            IServiceProvider services = serviceScope.ServiceProvider
                .Include(requestServices ?? Schema.Services);

            return new RequestServiceScope(services, serviceScope);
        }
    }
}
