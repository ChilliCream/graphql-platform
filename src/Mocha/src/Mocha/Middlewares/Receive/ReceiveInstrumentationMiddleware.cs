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
internal sealed class ReceiveInstrumentationMiddleware(IBusDiagnosticObserver observer)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        using var activity = observer.Receive(context);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            observer.OnReceiveError(context, ex);

            throw;
        }
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var observer = context.Services.GetRequiredService<IBusDiagnosticObserver>();
                var middleware = new ReceiveInstrumentationMiddleware(observer);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "ReceiveInstrumentation");
}
