using System;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore
{
    public abstract class QueryMiddlewareBase
    {
        private readonly RequestDelegate _next;
        private readonly QueryExecuter _queryExecuter;

        public QueryMiddlewareBase(
            RequestDelegate next,
            QueryExecuter queryExecuter)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _queryExecuter = queryExecuter
                ?? throw new ArgumentNullException(nameof(queryExecuter));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (CanHandleRequest(context))
            {
                await HandleRequestAsync(context, _queryExecuter)
                    .ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected abstract bool CanHandleRequest(HttpContext context);

        protected abstract Task<Execution.QueryRequest> CreateQueryRequest(
            HttpContext context);

        private async Task HandleRequestAsync(
            HttpContext context,
            QueryExecuter queryExecuter)
        {
            Execution.QueryRequest request = await CreateQueryRequest(context);

            IExecutionResult result = await queryExecuter
                .ExecuteAsync(request, context.RequestAborted)
                .ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task WriteResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            if (executionResult is IQueryExecutionResult queryResult)
            {
                string json = queryResult.ToJson();
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                await response.Body.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
