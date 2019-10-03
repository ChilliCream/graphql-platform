using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Server;
using HotChocolate.Utilities;
using Newtonsoft.Json;
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
                DateParseHandling = DateParseHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };

        public Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors,
            CancellationToken cancellationToken) =>
            FetchAsync(
                CreateRemoteRequest(request),
                httpClient,
                interceptors,
                cancellationToken);

        public async Task<QueryResult> FetchAsync(
            HttpQueryRequest request,
            HttpClient httpClient,
            IEnumerable<IHttpQueryRequestInterceptor> interceptors,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage message =
                await FetchInternalAsync(request, httpClient)
                    .ConfigureAwait(false);

            using (Stream stream = await message.Content.ReadAsStreamAsync()
                .ConfigureAwait(false))
            {
                object response = await BufferHelper.ReadAsync(
                    stream,
                    (buffer, bytesBuffered) =>
                        ParseJson(buffer, bytesBuffered),
                    cancellationToken)
                    .ConfigureAwait(false);

                QueryResult queryResult =
                    response is IReadOnlyDictionary<string, object> d
                        ? HttpResponseDeserializer.Deserialize(d)
                        : QueryResult.CreateError(
                            ErrorBuilder.New()
                                .SetMessage(
                                    "Could not deserialize query response.")
                                .Build());

                if (interceptors != null)
                {
                    foreach (IHttpQueryRequestInterceptor interceptor in
                        interceptors)
                    {
                        await interceptor.OnResponseReceivedAsync(
                            request, message, queryResult)
                            .ConfigureAwait(false);
                    }
                }

                return queryResult;
            }
        }

        private static object ParseJson(byte[] buffer, int bytesBuffered)
        {
            return Utf8GraphQLRequestParser.ParseJson(
                new ReadOnlySpan<byte>(buffer, 0, bytesBuffered));
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

        private async Task<(string, HttpResponseMessage)> FetchStringInternalAsync(
            HttpQueryRequest request,
            HttpClient httpClient)
        {
            var content = new StringContent(
                SerializeRemoteRequest(request),
                Encoding.UTF8,
                _json);

            HttpResponseMessage response =
                await httpClient.PostAsync(httpClient.BaseAddress, content)
                    .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return (json, response);
        }

        private async Task<HttpResponseMessage> FetchInternalAsync(
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

            return response;
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
