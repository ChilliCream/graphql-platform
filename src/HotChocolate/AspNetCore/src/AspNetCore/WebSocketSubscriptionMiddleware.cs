using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class WebSocketSubscriptionMiddleware : MiddlewareBase
{
    private readonly IServerDiagnosticEvents _diagnosticEvents;

    public WebSocketSubscriptionMiddleware(
        RequestDelegate next,
        IRequestExecutorProvider executorResolver,
        IRequestExecutorEvents executorEvents,
        IHttpResponseFormatter responseFormatter,
        IServerDiagnosticEvents diagnosticEvents,
        string schemaName)
        : base(next, executorResolver, executorEvents, responseFormatter, schemaName)
    {
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    public Task InvokeAsync(HttpContext context)
    {
        return context.WebSockets.IsWebSocketRequest
            ? HandleWebSocketSessionAsync(context)
            : NextAsync(context);
    }

    private async Task HandleWebSocketSessionAsync(HttpContext context)
    {
        if (!IsDefaultSchema)
        {
            context.Items[WellKnownContextData.SchemaName] = SchemaName;
        }

        using (_diagnosticEvents.WebSocketSession(context))
        {
            try
            {
                var executor = await GetExecutorAsync(context.RequestAborted);
                var interceptor = executor.GetRequiredService<ISocketSessionInterceptor>();
                context.Items[WellKnownContextData.RequestExecutor] = executor;
                await WebSocketSession.AcceptAsync(context, executor, interceptor);
            }
            catch (Exception ex)
            {
                _diagnosticEvents.WebSocketSessionError(context, ex);
            }
        }
    }
}
