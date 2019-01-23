using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotChocolate.Stitching
{
    public class RemoteQueryMiddleware
    {
        private readonly JsonSerializerSettings _jsonSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateParseHandling = DateParseHandling.DateTimeOffset
            };

        private readonly QueryDelegate _next;
        private readonly string _schemaName;

        public RemoteQueryMiddleware(QueryDelegate next, string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _schemaName = schemaName;
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            var httpClientFactory =
                context.Services.GetRequiredService<IHttpClientFactory>();

            context.Result = await FetchAsync(
                context.Request,
                httpClientFactory.CreateClient(_schemaName))
                .ConfigureAwait(false);
        }

        private async Task<QueryResult> FetchAsync(
            IReadOnlyQueryRequest request,
            HttpClient httpClient)
        {
            RemoteQueryRequest remoteRequest = CreateRemoteRequest(request);

            var content = new StringContent(
                SerializeRemoteRequest(remoteRequest),
                Encoding.UTF8,
                "application/json");

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
