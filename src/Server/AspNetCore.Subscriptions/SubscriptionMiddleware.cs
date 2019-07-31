using System;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMessagePipeline _messagePipeline;
        private readonly SubscriptionMiddlewareOptions _options;

        public SubscriptionMiddleware(
            RequestDelegate next,
            IMessagePipeline messagePipeline,
            SubscriptionMiddlewareOptions options)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _messagePipeline = messagePipeline
                ?? throw new ArgumentNullException(nameof(messagePipeline));
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest
                && IsValidPath(context, _options.Path))
            {
                await WebSocketSession
                    .New(context, _messagePipeline)
                    .HandleAsync(context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        public static bool IsValidPath(HttpContext context, PathString path)
        {
            return context.Request.Path.Equals(path,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
