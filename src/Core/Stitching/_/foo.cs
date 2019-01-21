using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private readonly FieldDelegate _next;
        private static readonly NameString _delegateName = "delegate";

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            IDirective directive = context.Field.Directives[_delegateName]
                .FirstOrDefault();

            if (directive != null)
            {
                // fetch data from remote schema
            }

            await _next.Invoke(context);
        }
    }

    public class RemoteQueryMiddleware
    {
        private readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };


        private QueryDelegate _next;

        public RemoteQueryMiddleware(QueryDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            var request = new QueryRequest(context.Request);



            context.Request = request.ToReadOnly();
        }

        private async Task FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient)
        {
            RemoteQueryRequest remoteRequest = CreateRemoteRequest(request);

            var content = new StringContent(
                SerializeRemoteRequest(remoteRequest),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response =
                await httpClient.PostAsync(string.Empty, content);


        }

        private RemoteQueryRequest CreateRemoteRequest(
            IReadOnlyQueryRequest request)
        {
            return new RemoteQueryRequest
            {
                Query = request.Query,
                OperationName = request.OperationName,
                Variables = request.VariableValues
            };
        }

        private string SerializeRemoteRequest(
            RemoteQueryRequest remoteRequest)
        {
            return JsonConvert.SerializeObject(
                remoteRequest, _jsonSettings);
        }
    }


    internal class RemoteQueryRequest
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public IReadOnlyDictionary<string, object> Variables { get; set; }
    }

    internal static class HttpResponseDeserializer
    {
        private const string _data = "data";
        private const string _extensions = "extensions";
        private const string _errors = "errors";

        public static IReadOnlyQueryResult Deserialize(
            JObject serializedResult)
        {
            var result = new QueryResult();

            DeserializeRootField(result, serializedResult, _data);
            DeserializeRootField(result, serializedResult, _extensions);
            DeserializeErrors(result, serializedResult);

            return result;
        }

        private static void DeserializeRootField(
            QueryResult result,
            JObject serializedResult,
            string field)
        {
            if (serializedResult.Property(field)?.Value is JObject obj)
            {
                foreach (KeyValuePair<string, object> item in
                    DeserializeObject(obj))
                {
                    result.Data[item.Key] = item.Value;
                }
            }
        }

        private static void DeserializeErrors(
            QueryResult result,
            JObject serializedResult)
        {
            if (serializedResult.Property(_errors)?.Value is JArray array)
            {
                foreach (JToken token in array.Children())
                {
                    // TODO : implement
                }
            }
        }

        private static object DeserializeToken(JToken token)
        {
            switch (token)
            {
                case JObject o:
                    return DeserializeObject(o);
                case JArray a:
                    return DeserializeList(a);
                case JValue v:
                    return DeserializeScalar(v);
                default:
                    throw new NotSupportedException();
            }
        }

        private static OrderedDictionary DeserializeObject(JObject obj)
        {
            var dict = new OrderedDictionary();

            foreach (JProperty property in obj.Properties())
            {
                dict[property.Name] = DeserializeToken(property.Value);
            }

            return dict;
        }

        private static List<object> DeserializeList(JArray array)
        {
            var list = new List<object>();

            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeToken(token));
            }

            return list;
        }

        private static object DeserializeScalar(JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Boolean:
                    return value.Value<bool>();

                case JTokenType.Integer:
                    return value.Value<long>();

                case JTokenType.Float:
                    return value.Value<decimal>();

                default:
                    return value.Value<string>();
            }
        }
    }
}
