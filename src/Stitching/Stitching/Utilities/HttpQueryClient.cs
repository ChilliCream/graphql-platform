using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching
{
    internal class HttpQueryClient
    {
        private const string _json = "application/json";

        private readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

        public Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request, HttpClient httpClient) =>
            FetchAsync(CreateRemoteRequest(request), httpClient);

        public async Task<QueryResult> FetchAsync(
            RemoteQueryRequest request,
            HttpClient httpClient)
        {
            var content = new StringContent(
                SerializeRemoteRequest(request),
                Encoding.UTF8,
                _json);

            HttpResponseMessage response =
                await httpClient.PostAsync(default(Uri), content)
                    .ConfigureAwait(false);

            string result = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return HttpResponseDeserializer.Deserialize(
                JsonConvert.DeserializeObject<JObject>(result, _jsonSettings));
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
}
