using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

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

            IServiceProvider services = (request.Services == null)
                ? _applicationServices.Include(Schema.Services)
                : _applicationServices.Include(request.Services);

            var context = new QueryContext(
                Schema,
                services,
                request.ToReadOnly());

            return ExecuteMiddlewareAsync(context);
        }

        private async Task<IExecutionResult> ExecuteMiddlewareAsync(
            IQueryContext context)
        {
            await _queryDelegate(context);

            if (context.Result == null)
            {
                // TODO : Resources
                throw new InvalidOperationException();
            }

            return context.Result;
        }
    }
}
