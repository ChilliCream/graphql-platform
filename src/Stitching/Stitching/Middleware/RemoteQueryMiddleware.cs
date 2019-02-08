using System;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
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

            context.Result = await _client.FetchAsync(
                context.Request,
                httpClientFactory.CreateClient(_schemaName))
                .ConfigureAwait(false);

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
