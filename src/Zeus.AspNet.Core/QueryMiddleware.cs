using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Zeus;
using Zeus.Abstractions;
using Zeus.Execution;
using Zeus.Parser;

namespace Zeus.AspNet
{
    public class QueryMiddleware
    {
        private const string _post = "Post";

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

        public async Task Invoke(
            HttpContext context,
            ISchema schema,
            IDocumentExecuter documentExecuter)
        {
            bool handled = false;
            if (context.Request.Method.Equals(_post, StringComparison.OrdinalIgnoreCase))
            {
                string path = context.Request.Path.ToUriComponent();
                if (_route == null || _route.Equals(path))
                {
                    await HandleRequestAsync(context, schema,
                        documentExecuter, context.RequestAborted)
                        .ConfigureAwait(false);
                    handled = true;
                }
            }

            if (!handled)
            {
                await _next(context);
            }
        }

        private async Task HandleRequestAsync(
            HttpContext context,
            ISchema schema,
            IDocumentExecuter documentExecuter,
            CancellationToken cancellationToken)
        {
            QueryRequest request = await ReadRequestAsync(context.Request)
                .ConfigureAwait(false);

            Dictionary<string, object> variables = ReadVariables(request);
            QueryDocument query = ReadQuery(request);

            QueryResult result = await documentExecuter.ExecuteAsync(
                schema, query, request.OperationName,
                variables, null, CancellationToken.None)
                .ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task<QueryRequest> ReadRequestAsync(HttpRequest request)
        {
            using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8))
            {
                string json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<QueryRequest>(json);
            }
        }

        private async Task WriteResponseAsync(HttpResponse response, QueryResult queryResult)
        {
            string json = JsonConvert.SerializeObject(queryResult);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await response.Body.WriteAsync(buffer, 0, buffer.Length);
        }

        private QueryDocument ReadQuery(QueryRequest request)
        {
            QueryDocumentReader queryDocumentReader = new QueryDocumentReader();
            return queryDocumentReader.Read(request.Query);
        }

        private Dictionary<string, object> ReadVariables(QueryRequest request)
            => string.IsNullOrEmpty(request.Variables)
                ? null
                : JsonConvert.DeserializeObject<Dictionary<string, object>>(request.Query);
    }
}
