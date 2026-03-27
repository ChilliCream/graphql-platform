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
/// When the transport registers <see cref="SchedulingTransportFeature"/>, this middleware is skipped
/// entirely during pipeline construction.
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

        var store = context.Services.GetRequiredService<IScheduledMessageStore>();
        if (context.Envelope is not null)
        {
            await store.PersistAsync(context.Envelope, scheduledTime, context.CancellationToken);
        }
    }

    /// <summary>
    /// Creates the middleware configuration that wires the scheduling middleware into the dispatch
    /// pipeline.
    /// </summary>
    /// <remarks>
    /// If the transport declares <see cref="SchedulingTransportFeature"/> with
    /// <see cref="SchedulingTransportFeature.SupportsSchedulingNatively"/> set to true
    /// </remarks>
    /// <returns>
    /// A <see cref="DispatchMiddlewareConfiguration"/> named "Scheduling" for pipeline registration.
    /// </returns>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                if (context.Transport.Features.Get<SchedulingTransportFeature>()?.SupportsSchedulingNatively is true)
                {
                    return next;
                }

                var appServices = context.Services.GetApplicationServices();
                var isService = appServices.GetService<IServiceProviderIsService>();
                if (isService?.IsService(typeof(IScheduledMessageStore)) is not true)
                {
                    return next;
                }

                var middleware = new DispatchSchedulingMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Scheduling");
}
