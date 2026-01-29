#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.AspNetCore.Subscriptions;
using Microsoft.AspNetCore.Http;
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
