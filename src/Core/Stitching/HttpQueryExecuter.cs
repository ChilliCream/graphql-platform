using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching
{
    public class HttpQueryExecuter
        : IQueryExecuter
    {
        private HttpClient _client;

        public HttpQueryExecuter(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public ISchema Schema => throw new NotImplementedException();

        public void Dispose()
        {

        }

        public async Task<IExecutionResult> ExecuteAsync(
            QueryRequest request,
            CancellationToken cancellationToken)
        {
            var remoteRquest = new ClientQueryRequest
            {
                Query = request.Query,
                OperationName = request.OperationName,
                Variables = request.VariableValues
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(remoteRquest,
                    new JsonSerializerSettings
                    {
                        ContractResolver =
                            new CamelCasePropertyNamesContractResolver()
                    }),
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await _client.PostAsync("", content);

            string json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ClientQueryResult>(json);

            OrderedDictionary data = result.Data == null
                ? null
                : result.Data.ToDictionary();

            return new QueryResult(data);
        }
    }

    internal class ClientQueryRequest
    {
        public string OperationName { get; set; }
        public string NamedQuery { get; set; }
        public string Query { get; set; }
        public IReadOnlyDictionary<string, object> Variables { get; set; }
    }

    public class ClientQueryResult
    {
        public JObject Data { get; set; }
        // TODO : error serialization
        // public List<QueryError> Errors { get; set; }
    }

    internal static class ResultSerializationUtilities
    {
        public static OrderedDictionary ToDictionary(
            this JObject input)
        {
            if (input == null)
            {
                return null;
            }

            return DeserializeObject(input.Properties());
        }

        private static OrderedDictionary DeserializeObject(
            IEnumerable<JProperty> properties)
        {
            if (properties == null)
            {
                return null;
            }

            var values = new OrderedDictionary();

            foreach (JProperty property in properties)
            {
                values[property.Name] = DeserializeValue(property.Value);
            }

            return values;
        }

        private static object DeserializeValue(object value)
        {
            if (value is JObject jo)
            {
                return DeserializeObject(jo.Properties());
            }

            if (value is JArray ja)
            {
                return DeserializeList(ja);
            }

            if (value is JValue jv)
            {
                return DeserializeScalar(jv);
            }

            throw new NotSupportedException();
        }

        private static List<object> DeserializeList(JArray array)
        {
            var list = new List<object>();

            foreach (JToken token in array.Children())
            {
                list.Add(DeserializeValue(token));
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
                    return value.Value<int>();
                case JTokenType.Float:
                    return value.Value<double>();
                default:
                    return value.Value<string>();
            }
        }
    }
}
