using System;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Pipeline
{
    internal class HttpRequestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpRequestClient _httpRequestClient;

        public HttpRequestMiddleware(
            RequestDelegate next,
            HttpRequestClient httpRequestClient)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _httpRequestClient = httpRequestClient;
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            context.Result =
                await _httpRequestClient.FetchAsync(
                    context.Request,
                    context.Schema.Name,
                    context.RequestAborted)
                    .ConfigureAwait(false);

            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
