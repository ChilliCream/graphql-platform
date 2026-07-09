using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Middlewares;

/// <summary>
/// Creates receive spans/metrics around the entire receive pipeline.
/// </summary>
/// <remarks>
/// Errors are reported to diagnostics and then rethrown so reliability middleware can still decide
/// settlement behavior.
/// Without this middleware, receive-side failures become much harder to correlate with transport,
/// endpoint, and handler latency behavior.
/// </remarks>
internal sealed class ReceiveInstrumentationMiddleware(IMessagingDiagnosticEvents events)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        using var activity = events.Receive(context);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            events.ReceiveError(context, ex);

            throw;
        }
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var events = context.Services.GetRequiredService<IMessagingDiagnosticEvents>();
                var middleware = new ReceiveInstrumentationMiddleware(events);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "ReceiveInstrumentation");
}
