using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AspNetCore
{
    public class QueryMiddleware
    {
        private const string _post = "Post";
        private static readonly Parser _parser = Parser.Default;

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
            Schema schema,
            OperationExecuter operationExecuter)
        {
            bool handled = false;
            if (context.Request.Method.Equals(_post, StringComparison.OrdinalIgnoreCase))
            {
                string path = context.Request.Path.ToUriComponent();
                if (_route == null || _route.Equals(path))
                {
                    await HandleRequestAsync(context, schema,
                        operationExecuter, context.RequestAborted)
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
            Schema schema,
            OperationExecuter operationExecuter,
            CancellationToken cancellationToken)
        {
            QueryRequest request = await ReadRequestAsync(context.Request)
                .ConfigureAwait(false);

            DocumentNode queryDocument = _parser.Parse(request.Query);

            QueryResult result = await operationExecuter.ExecuteRequestAsync(
                schema, queryDocument, request.OperationName,
                DeserializeVariables(request.Variables), null,
                CancellationToken.None).ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task<QueryRequest> ReadRequestAsync(HttpRequest request)
        {
            using (StreamReader reader = new StreamReader(
                request.Body, Encoding.UTF8))
            {
                string json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<QueryRequest>(json);
            }
        }

        private async Task WriteResponseAsync(HttpResponse response, QueryResult queryResult)
        {
            Dictionary<string, object> internalResult = new Dictionary<string, object>();

            if (queryResult.Data != null)
            {
                internalResult["data"] = queryResult.Data;
            }

            if (queryResult.Errors != null)
            {
                internalResult["errors"] = queryResult.Errors;
            }

            string json = JsonConvert.SerializeObject(internalResult);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await response.Body.WriteAsync(buffer, 0, buffer.Length);
        }

        private Dictionary<string, object> DeserializeVariables(
            Dictionary<string, object> input)
        {
            if (input == null)
            {
                return null;
            }

            foreach (string key in input.Keys.ToArray())
            {
                input[key] = DeserializeVariableValue(input[key]);
            }
            return input;
        }

        private object DeserializeVariableValue(object value)
        {
            if (value is JObject jo)
            {
                return DeserializeVariables(
                    jo.ToObject<Dictionary<string, object>>());
            }

            if (value is JArray ja)
            {
                List<object> list = new List<object>();
                foreach (JToken token in ja.Children())
                {
                    list.Add(DeserializeVariableValue(token));
                }
                return list;
            }

            if (value is JValue jv)
            {
                return jv.Value<object>();
            }

            return value;
        }
    }
}
