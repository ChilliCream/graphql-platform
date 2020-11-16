using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class WebSocketSubscriptionMiddleware : MiddlewareBase
    {
        public WebSocketSubscriptionMiddleware(
            RequestDelegate next,
            IRequestExecutorResolver executorResolver,
            IHttpResultSerializer resultSerializer,
            NameString schemaName)
            : base(next, executorResolver, resultSerializer, schemaName)
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
