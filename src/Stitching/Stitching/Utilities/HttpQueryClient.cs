using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
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
            HttpResponseMessage message =
                await FetchInternalAsync(request, httpClient)
                    .ConfigureAwait(false);

            object response;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 2);
            int bytesBuffered = 0;

            try
            {
                using (Stream stream = await message.Content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        var bytesRemaining = buffer.Length - bytesBuffered;

                        if (bytesRemaining == 0)
                        {
                            var next = ArrayPool<byte>.Shared.Rent(
                                buffer.Length * 2);
                            Buffer.BlockCopy(buffer, 0, next, 0, buffer.Length);
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = next;
                            bytesRemaining = buffer.Length - bytesBuffered;
                        }

                        var bytesRead = await stream.ReadAsync(
                            buffer, bytesBuffered, bytesRemaining)
                            .ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        bytesBuffered += bytesRead;
                    }
                }

                response = ParseJson(buffer, bytesBuffered);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            QueryResult queryResult =
                response is IReadOnlyDictionary<string, object> d
                    ? HttpResponseDeserializer.Deserialize(d)
                    : QueryResult.CreateError(
                        ErrorBuilder.New()
                            .SetMessage("Could not deserialize query response.")
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

        private object ParseJson(byte[] buffer, int bytesBuffered)
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
