using System;
using System.Collections.Generic;
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
                DateParseHandling = DateParseHandling.None
            };

        public Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors) =>
            FetchAsync(CreateRemoteRequest(request), httpClient, interceptors);

        public async Task<QueryResult> FetchAsync(
            HttpQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors)
        {
            (string json, HttpResponseMessage message) result =
                await FetchStringAsync(request, httpClient)
                    .ConfigureAwait(false);

            QueryResult queryResult = HttpResponseDeserializer.Deserialize(
                JsonConvert.DeserializeObject<JObject>(
                    result.json, _jsonSettings));

            if (interceptors != null)
            {
                foreach (IHttpQueryRequestInterceptor interceptor in
                    interceptors)
                {
                    await interceptor.OnResponseReceivedAsync(
                        request, result.message, queryResult)
                        .ConfigureAwait(false);
                }
            }

            return queryResult;
        }

        public Task<(string, HttpResponseMessage)> FetchStringAsync(
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

        private async Task<(string, HttpResponseMessage)>
            FetchStringInternalAsync(
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
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return (json, response);
        }

        private HttpQueryRequest CreateRemoteRequest(
            IReadOnlyQueryRequest request)
        {
            return new HttpQueryRequest
            {
                Query = request.Query.ToString(),
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
