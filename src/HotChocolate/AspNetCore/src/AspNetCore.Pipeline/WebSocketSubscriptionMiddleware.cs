using HotChocolate.AspNetCore.Subscriptions;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

public sealed class WebSocketSubscriptionMiddleware : MiddlewareBase
{
    public WebSocketSubscriptionMiddleware(
        HttpRequestDelegate next,
        HttpRequestExecutorProxy executor)
        : base(next, executor)
    {
    }

    public Task InvokeAsync(HttpContext context)
    {
        return context.WebSockets.IsWebSocketRequest
            ? HandleWebSocketSessionAsync(context)
            : NextAsync(context);
    }

    private async Task HandleWebSocketSessionAsync(HttpContext context)
    {
        var session = await Executor.GetOrCreateSessionAsync(context.RequestAborted);

        using (session.DiagnosticEvents.WebSocketSession(context))
        {
            try
            {
                await WebSocketSession.AcceptAsync(context, session);
            }
            catch (Exception ex)
            {
                session.DiagnosticEvents.WebSocketSessionError(context, ex);
            }
        }
    }
}
