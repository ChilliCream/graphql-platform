using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution.Instrumentation;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public class WebSocketSubscriptionMiddleware : MiddlewareBase
{
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public WebSocketSubscriptionMiddleware(
        RequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        IServerDiagnosticEvents diagnosticEvents,
        NameString schemaName)
        : base(next, executorResolver, resultSerializer, schemaName)
    {
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            return HandleWebSocketSessionAsync(context);
        }
        else
        {
            return NextAsync(context);
        }
    }

    private async Task HandleWebSocketSessionAsync(HttpContext context)
    {
        using (_diagnosticEvents.WebSocketSession(context))
        {
            try
            {
                IRequestExecutor executor = await GetExecutorAsync(context.RequestAborted);
                var messagePipeline = executor.GetRequiredService<IMessagePipeline>();
                var socketSessionInterceptor = executor.GetRequiredService<ISocketSessionInterceptor>();

                await WebSocketSession
                    .New(context, messagePipeline, socketSessionInterceptor)
                    .HandleAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _diagnosticEvents.WebSocketSessionError(context, ex);
            }
        }
    }
}
