using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteQueryMiddleware
    {
        private readonly HttpQueryClient _client = new HttpQueryClient();
        private readonly QueryDelegate _next;
        private readonly string _schemaName;

        public RemoteQueryMiddleware(QueryDelegate next, string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    StitchingResources.SchemaName_EmptyOrNull,
                    nameof(schemaName));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));
            _schemaName = schemaName;
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            IHttpClientFactory httpClientFactory =
                context.Services.GetRequiredService<IHttpClientFactory>();

            context.Result = await _client.FetchAsync(
                context.Request,
                httpClientFactory.CreateClient(_schemaName),
                context.Services.GetServices<IHttpQueryRequestInterceptor>(),
                context.RequestAborted)
                .ConfigureAwait(false);

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
