using System.Threading.Tasks;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

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
            IMessagePipeline? messagePipeline = executor.Services.GetRequiredService<IMessagePipeline>();
            ISocketSessionInterceptor? socketSessionInterceptor =
                executor.Services.GetRequiredService<ISocketSessionInterceptor>();

            await WebSocketSession
                .New(context, messagePipeline, socketSessionInterceptor)
                .HandleAsync(context.RequestAborted);
        }
        else
        {
            await NextAsync(context);
        }
    }
}
