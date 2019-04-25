using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal class QueryExecutor
        : IQueryExecutor
    {
        private readonly IServiceProvider _applicationServices;
        private readonly QueryDelegate _queryDelegate;
        private readonly FieldMiddlewareCompiler _fieldMiddlewareCompiler;
        private bool _disposed;

        public QueryExecutor(
            ISchema schema,
            IServiceProvider applicationServices,
            QueryDelegate queryDelegate,
            FieldMiddleware fieldMiddleware)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _applicationServices = applicationServices
                ?? throw new ArgumentNullException(nameof(applicationServices));
            _queryDelegate = queryDelegate
                ?? throw new ArgumentNullException(nameof(queryDelegate));

            if (Schema.Services != null)
            {
                IEnumerable<IDiagnosticObserver> observers = Schema.Services
                    .GetService<IEnumerable<IDiagnosticObserver>>();

                if (observers != null)
                {
                    QueryExecutionDiagnostics diagnosticEvents = _applicationServices
                        .GetService<QueryExecutionDiagnostics>();
                    diagnosticEvents.Subscribe(observers);
                }
            }

            _fieldMiddlewareCompiler = new FieldMiddlewareCompiler(
                schema, fieldMiddleware);
        }

        public ISchema Schema { get; }

        public Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
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
                request,
                fs => _fieldMiddlewareCompiler.GetMiddleware(fs.Field));

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
                    return QueryResult.CreateError(new Error
                    {
                        Message = CoreResources.QueryExecutor_NoResult
                    });
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
            IServiceProvider services = requestServices ?? Schema.Services;

            if (services == null)
            {
                return new RequestServiceScope(
                    serviceScope.ServiceProvider,
                    serviceScope);
            }

            services = serviceScope.ServiceProvider.Include(services);
            return new RequestServiceScope(services, serviceScope);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _applicationServices is IDisposable d)
                {
                    d.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
