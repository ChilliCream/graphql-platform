using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        private readonly List<QueryMiddleware> _middlewareComponents =
            new List<QueryMiddleware>();

        public IServiceCollection Services =>
            throw new System.NotImplementedException();

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareComponents.Add(middleware);
            return this;
        }

        public IQueryExecuter BuildQueryExecuter()
        {
            throw new System.NotImplementedException();
        }
    }

    public class QueryExecuter
        : IQueryExecuter
    {
        private readonly QueryDelegate _queryDelegate;

        public QueryExecuter(ISchema schema, QueryDelegate queryDelegate)
        {
            Schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
            _queryDelegate = queryDelegate
                ?? throw new ArgumentNullException(nameof(queryDelegate));
        }

        public ISchema Schema { get; }

        public Task<IExecutionResult> ExecuteAsync(
            QueryRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var context = new QueryContext(Schema, request.ToReadOnly());
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
