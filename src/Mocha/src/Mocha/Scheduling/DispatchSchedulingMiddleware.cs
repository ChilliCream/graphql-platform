using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Dispatch middleware that intercepts outgoing messages with a scheduled time and persists them
/// to the scheduled message store instead of forwarding them to the next pipeline stage.
/// </summary>
/// <remarks>
/// Messages without a <see cref="IDispatchContext.ScheduledTime"/> and dispatches marked with
/// <see cref="SchedulingMiddlewareFeature.SkipScheduler"/> pass through to the next middleware.
/// Scheduled messages are routed to the registered store for the current transport.
/// </remarks>
public sealed class DispatchSchedulingMiddleware
{
    /// <summary>
    /// Evaluates whether the message should be persisted to the scheduled message store or
    /// forwarded down the pipeline.
    /// </summary>
    /// <param name="context">The current dispatch context containing the message envelope and metadata.</param>
    /// <param name="next">The next middleware delegate in the dispatch pipeline.</param>
    /// <returns>A value task that completes when the message has been persisted or forwarded.</returns>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.ScheduledTime is not { } scheduledTime)
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

        if (context.Envelope is null)
        {
            await next(context);
            return;
        }

        if (context.Envelope.ScheduledTime != scheduledTime)
        {
            context.Envelope = new MessageEnvelope(context.Envelope) { ScheduledTime = scheduledTime };
        }

        var resolver = context.Services.GetRequiredService<ScheduledMessageStoreResolver>();

        if (!resolver.TryGetForDispatch(context, out var store))
        {
            throw ThrowHelper.ScheduledDispatchUnsupported(context.Transport.GetType());
        }

        var token = await store.PersistAsync(context, context.CancellationToken);

        context.Features.Configure<ScheduledMessageFeature>(f => f.Token = token);
    }

    /// <summary>
    /// Creates the middleware configuration that wires the scheduling middleware into the dispatch
    /// pipeline.
    /// </summary>
    /// <returns>
    /// A <see cref="DispatchMiddlewareConfiguration"/> named "Scheduling" for pipeline registration.
    /// </returns>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new DispatchSchedulingMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Scheduling");
}
