using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public class ParseSingleResultMiddleware
    {
        private readonly HttpOperationDelegate _next;

        public ParseSingleResultMiddleware(HttpOperationDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpResponse != null && !context.Result.IsDataOrErrorModified)
            {
                context.HttpResponse.EnsureSuccessStatusCode();

                using (var stream = await context.HttpResponse.Content.ReadAsStreamAsync()
                    .ConfigureAwait(false))
                {
                    await context.ResultParser.ParseAsync(
                        stream, context.Result, context.RequestAborted)
                        .ConfigureAwait(false);
                }
            }

            await _next(context);
        }
    }
}
