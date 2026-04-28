using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Dispatch middleware that intercepts outgoing messages with a scheduled time and routes them
/// to the appropriate <see cref="IScheduledMessageStore"/> via <see cref="IScheduledMessageStoreResolver"/>.
/// </summary>
/// <remarks>
/// Messages without a <see cref="IDispatchContext.ScheduledTime"/>, dispatches marked with
/// <see cref="SchedulingMiddlewareFeature.SkipScheduler"/>, or contexts without a built envelope
/// pass through to the next middleware. When a scheduled dispatch targets a transport with no
/// registered store and no fallback store, a <see cref="NotSupportedException"/> is thrown rather
/// than silently sending the message immediately.
/// </remarks>
public sealed class DispatchSchedulingMiddleware
{
    /// <summary>
    /// Routes the dispatch through the scheduled-message store resolver when the message carries
    /// a scheduled time, or forwards to the next middleware otherwise.
    /// </summary>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.ScheduledTime is null)
        {
            await next(context);
            return;
        }

        if (context.Envelope is null)
        {
            await next(context);
            return;
        }

        var feature = context.Features.GetOrSet<SchedulingMiddlewareFeature>();
        if (feature.SkipScheduler)
        {
            await next(context);
            return;
        }

        var resolver = context.Services.GetRequiredService<IScheduledMessageStoreResolver>();
        if (!resolver.TryGetForDispatch(context, out var store))
        {
            throw new NotSupportedException(
                "Scheduled dispatch is not supported for transport "
                + $"'{context.Transport.GetType().Name}'. Register a scheduled-message store for the "
                + "transport, or configure a fallback scheduling store.");
        }

        var token = await store.PersistAsync(context, context.CancellationToken);

        context.Features.Configure<ScheduledMessageFeature>(f => f.Token = token);
    }

    /// <summary>
    /// Creates the middleware configuration that wires the scheduling middleware into the dispatch
    /// pipeline. The middleware is always installed; resolver lookup happens at invocation time.
    /// </summary>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchSchedulingMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Scheduling");
}
