using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Middleware
{
    public class SendHttpRequestMiddleware
    {
        private readonly OperationDelegate _next;

        public SendHttpRequestMiddleware(OperationDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IHttpOperationContext context)
        {
            if (context.HttpResponse is null)
            {
                context.HttpResponse = await context.Client.SendAsync(
                    context.HttpRequest, context.RequestAborted)
                    .ConfigureAwait(false);
            }

            await _next(context);
        }
    }
}
