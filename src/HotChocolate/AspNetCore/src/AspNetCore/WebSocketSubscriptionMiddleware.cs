using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class WebSocketSubscriptionMiddleware : MiddlewareBase
    {
        private IMessagePipeline? _messagePipeline;
        private bool _disposed;

        public WebSocketSubscriptionMiddleware(
            RequestDelegate next,
            IRequestExecutorResolver executorResolver,
            NameString schemaName)
            : base(next, executorResolver, schemaName)
        {
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                IRequestExecutor executor = await GetExecutorAsync(context.RequestAborted);
                var messagePipeline = executor.Services.GetRequiredService<IMessagePipeline>();

                await WebSocketSession
                    .New(context, messagePipeline)
                    .HandleAsync(context.RequestAborted);
            }
            else
            {
                await NextAsync(context);
            }
        }
    }
}
