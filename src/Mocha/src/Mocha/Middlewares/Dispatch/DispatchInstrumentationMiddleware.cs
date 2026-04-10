using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Wraps dispatch execution in diagnostic instrumentation and propagates activity metadata to
/// outgoing headers.
/// </summary>
/// <remarks>
/// Without activity propagation, downstream services lose causal trace continuity for produced
/// messages.
/// </remarks>
internal sealed class DispatchInstrumentationMiddleware(IMessagingDiagnosticEvents events)
{
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        using var scope = events.Dispatch(context);

        context.Headers.WithActivity();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            events.DispatchError(context, ex);
            throw;
        }
    }

    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var events = context.Services.GetRequiredService<IMessagingDiagnosticEvents>();
                var middleware = new DispatchInstrumentationMiddleware(events);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Instrumentation");
}
