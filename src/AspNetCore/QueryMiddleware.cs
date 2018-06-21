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
            Schema schema)
        {
            if (context.Request.IsGet() || context.Request.IsPost())
            {
                string path = context.Request.Path.ToUriComponent();
                if (_route == null || _route.Equals(path))
                {
                    QueryRequest request = context.Request.IsGet()
                        ? GetRequest.ReadRequest(context)
                        : await PostRequest.ReadRequestAsync(context);

                    await HandleRequestAsync(context, request,
                            schema, context.RequestAborted)
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
            QueryRequest request,
            Schema schema,
            CancellationToken cancellationToken)
        {
            QueryResult result = await schema.ExecuteAsync(
                request.Query, request.OperationName,
                DeserializeVariables(request.Variables), null,
                cancellationToken).ConfigureAwait(false);

            await WriteResponseAsync(context.Response, result)
                .ConfigureAwait(false);
        }

        private async Task WriteResponseAsync(HttpResponse response, QueryResult queryResult)
        {
            string json = queryResult.ToString(false);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await response.Body.WriteAsync(buffer, 0, buffer.Length);
        }

        private Dictionary<string, IValueNode> DeserializeVariables(
            Dictionary<string, JToken> input)
        {
            if (input == null)
            {
                return null;
            }

            Dictionary<string, IValueNode> values =
                new Dictionary<string, IValueNode>();
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

            List<ObjectFieldNode> fields = new List<ObjectFieldNode>();
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
                List<IValueNode> list = new List<IValueNode>();
                foreach (JToken token in ja.Children())
                {
                    list.Add(DeserializeVariableValue(token));
                }
                return new ListValueNode(null, list);
            }

            if (value is JValue jv)
            {
                switch (jv.Type)
                {
                    case JTokenType.Boolean:
                        return new BooleanValueNode(jv.Value<bool>());
                    case JTokenType.Integer:
                        return new IntValueNode(jv.Value<string>());
                    case JTokenType.Float:
                        return new FloatValueNode(jv.Value<string>());
                    default:
                        return new StringValueNode(jv.Value<string>());
                }
            }

            throw new NotSupportedException();
        }
    }
}
