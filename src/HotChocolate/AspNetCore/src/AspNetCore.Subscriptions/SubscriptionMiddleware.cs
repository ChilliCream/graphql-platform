using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMessagePipeline _messagePipeline;

        public SubscriptionMiddleware(
            RequestDelegate next,
            IMessagePipeline messagePipeline)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _messagePipeline = messagePipeline
                ?? throw new ArgumentNullException(nameof(messagePipeline));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
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
    }
}
