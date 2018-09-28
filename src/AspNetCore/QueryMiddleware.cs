using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

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
            if (context.Request.IsGet() || context.Request.IsPost())
            {
                string path = context.Request.Path.ToUriComponent();
                if (_route == null || _route.Equals(path))
                {
                    await HandleRequestAsync(context, queryExecuter,
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
                    VariableValues = DeserializeVariables(request.Variables),
                    Services = CreateRequestServices(context)
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

        private Dictionary<string, object> DeserializeVariables(
            JObject input)
        {
            if (input == null)
            {
                return null;
            }

            return DeserializeVariables(input.ToObject<Dictionary<string, JToken>>());
        }

        private Dictionary<string, object> DeserializeVariables(
            Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var values = new Dictionary<string, object>();
            foreach (string key in input.Keys.ToArray())
            {
                values[key] = DeserializeVariableValue(input[key]);
            }
            return values;
        }

        private ObjectValueNode DeserializeObjectValue(
           Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            var fields = new List<ObjectFieldNode>();
            foreach (string key in input.Keys.ToArray())
            {
                fields.Add(new ObjectFieldNode(null,
                    new NameNode(null, key),
                    DeserializeVariableValue(input[key])));
            }
            return new ObjectValueNode(null, fields);
        }

        private IValueNode DeserializeVariableValue(object value)
        {
            if (value is JObject jo)
            {
                return DeserializeObjectValue(
                    jo.ToObject<Dictionary<string, JToken>>());
            }

            if (value is JArray ja)
            {
                return DeserializeVariableListValue(ja);
            }

            if (value is JValue jv)
            {
                return DeserializeVariableScalarValue(jv);
            }

            throw new NotSupportedException();
        }

        private IValueNode DeserializeVariableListValue(JArray array)
        {
            var list = new List<IValueNode>();
            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeVariableValue(token));
            }
            return new ListValueNode(null, list);
        }

        private IValueNode DeserializeVariableScalarValue(JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return new BooleanValueNode(value.Value<bool>());
                case JTokenType.Integer:
                    return new IntValueNode(value.Value<string>());
                case JTokenType.Float:
                    return new FloatValueNode(value.Value<string>());
                default:
                    return new StringValueNode(value.Value<string>());
            }
        }

        private IServiceProvider CreateRequestServices(HttpContext context)
        {
            Dictionary<Type, object> services = new Dictionary<Type, object>
            {
                { typeof(HttpContext), context }
            };

            return new RequestServiceProvider(
                context.RequestServices, services);
        }
    }
}
