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
internal sealed class DispatchInstrumentationMiddleware(IBusDiagnosticObserver observer)
{
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        using var activity = observer.Dispatch(context);

        context.Headers.WithActivity();

        await next(context);
    }

    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var observer = context.Services.GetRequiredService<IBusDiagnosticObserver>();
                var middleware = new DispatchInstrumentationMiddleware(observer);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Instrumentation");
}
