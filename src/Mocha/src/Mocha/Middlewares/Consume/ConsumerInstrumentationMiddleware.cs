using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Middlewares;

/// <summary>
/// Captures diagnostics around consumer execution.
/// </summary>
/// <remarks>
/// Consumer instrumentation is separate from receive instrumentation to distinguish transport-level
/// work from handler-level work in traces and metrics.
/// Without this separation, high receive latency and high handler latency are hard to attribute and
/// tune independently.
/// </remarks>
internal sealed class ConsumerInstrumentationMiddleware(IBusDiagnosticObserver observer)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        using var scope = observer.Consume(context);

        await next(context);
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var observer = context.Services.GetRequiredService<IBusDiagnosticObserver>();
                var middleware = new ConsumerInstrumentationMiddleware(observer);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Instrumentation");
}
