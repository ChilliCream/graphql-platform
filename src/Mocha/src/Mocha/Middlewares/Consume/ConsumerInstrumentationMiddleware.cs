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
internal sealed class ConsumerInstrumentationMiddleware(IMessagingDiagnosticEvents events)
{
    public async ValueTask InvokeAsync(IConsumeContext context, ConsumerDelegate next)
    {
        using var scope = events.Consume(context);

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            events.ConsumeError(context, ex);
            throw;
        }
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var events = context.Services.GetRequiredService<IMessagingDiagnosticEvents>();
                var middleware = new ConsumerInstrumentationMiddleware(events);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Instrumentation");
}
