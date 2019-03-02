using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching.Utilities
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
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            string result = await FetchStringAsync(request, httpClient)
                .ConfigureAwait(false);

            return HttpResponseDeserializer.Deserialize(
                JsonConvert.DeserializeObject<JObject>(result, _jsonSettings));
        }

        public Task<string> FetchStringAsync(
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            return FetchStringInternalAsync(request, httpClient);
        }

        private async Task<string> FetchStringInternalAsync(
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            var content = new StringContent(
                SerializeRemoteRequest(request),
                Encoding.UTF8,
                _json);

            HttpResponseMessage response =
                await httpClient.PostAsync(default(Uri), content)
                    .ConfigureAwait(false);

            return await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
        }

        private HttpQueryRequest CreateRemoteRequest(
            IReadOnlyQueryRequest request)
        {
            return new HttpQueryRequest
            {
                Query = request.Query,
                OperationName = request.OperationName,
                Variables = request.VariableValues
            };
        }

        private string SerializeRemoteRequest(
            HttpQueryRequest remoteRequest)
        {
            return JsonConvert.SerializeObject(
                remoteRequest, _jsonSettings);
        }
    }
}
