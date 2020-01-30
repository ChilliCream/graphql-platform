using System;
using System.Threading;
using System.Threading.Tasks;
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
            : this(schema, queryDelegate, fieldMiddleware)
        {
            _applicationServices = applicationServices
                ?? throw new ArgumentNullException(nameof(applicationServices));
        }

        public QueryExecutor(
            ISchema schema,
            QueryDelegate queryDelegate,
            FieldMiddleware fieldMiddleware)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _queryDelegate = queryDelegate
                ?? throw new ArgumentNullException(nameof(queryDelegate));

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

            IRequestServiceScope serviceScope = CreateServiceScope(request.Services);

            var context = new QueryContext(
                Schema,
                serviceScope,
                request,
                (field, selection) => _fieldMiddlewareCompiler.GetMiddleware(field),
                cancellationToken);

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
            if (_applicationServices is null)
            {
                return new RequestServiceScope(
                    CreateRequestServices(requestServices),
                    Disposable.Instance);
            }
            else
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
        }

        private IServiceProvider CreateRequestServices(
            IServiceProvider requestServices) =>
            requestServices ?? Schema.Services;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing
                    && _applicationServices != null
                    && _applicationServices is IDisposable d)
                {
                    d.Dispose();
                }
                _disposed = true;
            }
        }

        private class Disposable
            : IDisposable
        {
            public void Dispose()
            {
            }

            public static Disposable Instance { get; } = new Disposable();
        }
    }
}
