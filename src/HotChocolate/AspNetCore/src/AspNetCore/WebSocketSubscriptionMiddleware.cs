using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Subscriptions;
using Microsoft.Extensions.Hosting;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public class WebSocketSubscriptionMiddleware : MiddlewareBase
{
    private readonly IServerDiagnosticEvents _diagnosticEvents;
    private readonly IHostApplicationLifetime _hostLifetime;

    public WebSocketSubscriptionMiddleware(
        RequestDelegate next,
        IRequestExecutorResolver executorResolver,
        IHttpResultSerializer resultSerializer,
        IServerDiagnosticEvents diagnosticEvents,
        IHostApplicationLifetime hostLifetime,
        NameString schemaName)
        : base(next, executorResolver, resultSerializer, schemaName)
    {
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _hostLifetime = hostLifetime;
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
        if (!IsDefaultSchema)
        {
            context.Items[WellKnownContextData.SchemaName] = SchemaName.Value;
        }

        using (_diagnosticEvents.WebSocketSession(context))
        {
            try
            {
                IRequestExecutor requestExecutor = 
                    await GetExecutorAsync(context.RequestAborted);
                IMessagePipeline? messagePipeline = 
                    requestExecutor.GetRequiredService<IMessagePipeline>();
                ISocketSessionInterceptor? socketSessionInterceptor = 
                    requestExecutor.GetRequiredService<ISocketSessionInterceptor>();
                context.Items[WellKnownContextData.RequestExecutor] = requestExecutor;

                await WebSocketSession
                    .New(context,
                        messagePipeline,
                        socketSessionInterceptor,
                        _hostLifetime.ApplicationStopping)
                    .HandleAsync(context.RequestAborted);
            }
            catch (Exception ex)
            {
                _diagnosticEvents.WebSocketSessionError(context, ex);
            }
        }
    }
}
