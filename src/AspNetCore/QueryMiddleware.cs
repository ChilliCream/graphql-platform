using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore
{
    public class QueryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _route;

        public QueryMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public QueryMiddleware(RequestDelegate next, string route)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _route = route;
        }

        public async Task InvokeAsync(
            HttpContext context,
            QueryExecuter queryExecuter)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                // await Test(context, await context.WebSockets.AcceptWebSocketAsync("graphql-ws"), queryExecuter);
                var session = await WebSocketSession.TryCreateAsync(
                    context, queryExecuter);
                await session.StartAsync(context.RequestAborted);
            }

            if (context.Request.IsGet() || context.Request.IsPost())
            {
                string path = context.Request.Path.ToUriComponent();
                if (_route == null || _route.Equals(path))
                {
                    await HandleRequestAsync(
                            context,
                            queryExecuter,
                            context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            QueryExecuter queryExecuter,
            CancellationToken cancellationToken)
        {
            QueryRequest request = context.Request.IsGet()
                ? GetRequest.ReadRequest(context)
                : await PostRequest.ReadRequestAsync(context);

            IExecutionResult result = await queryExecuter.ExecuteAsync(
                new Execution.QueryRequest(request.Query, request.OperationName)
                {
                    VariableValues = QueryMiddlewareUtilities
                        .DeserializeVariables(request.Variables),
                    Services = QueryMiddlewareUtilities
                        .CreateRequestServices(context)
                },
                cancellationToken).ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task WriteResponseAsync(
            HttpResponse response,
            IExecutionResult executionResult)
        {
            if (executionResult is IQueryExecutionResult queryResult)
            {
                // TODO : refactor this, we dont need this string...
                string json = queryResult.ToJson();
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                await response.Body.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
