using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Outbox;

namespace Mocha.Scheduling;

/// <summary>
/// Dispatch middleware that intercepts outgoing messages with a scheduled time and persists them
/// to the scheduled message store instead of forwarding them to the next pipeline stage.
/// </summary>
/// <remarks>
/// Messages without a <see cref="IDispatchContext.ScheduledTime"/> pass through to the next middleware.
/// Scheduled messages are routed to the registered store for the current transport. A scheduled message of a
/// dispatch that carries an <see cref="IMessageOutbox"/> feature is persisted in that outbox instead.
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
    /// <exception cref="InvalidOperationException">A scheduled message has no envelope.</exception>
    public async ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.ScheduledTime is not { } scheduledTime)
        {
            await next(context);
            return;
        }

        if (context.Envelope is null)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        if (context.Envelope.ScheduledTime != scheduledTime)
        {
            context.Envelope = context.Envelope.WithScheduledTime(scheduledTime);
        }

        // Resolved first, so that a transport without scheduling support throws before the outbox stores anything.
        var store = ResolveStore(context);

        if (context.Features.Get<IMessageOutbox>() is { } outbox)
        {
            await outbox.PersistAsync(context.Envelope, context.CancellationToken);
            return;
        }

        var token = await store.PersistAsync(context, context.CancellationToken);

        context.Features.Configure<ScheduledMessageFeature>(f => f.Token = token);
    }

    internal static IScheduledMessageStore ResolveStore(IDispatchContext context)
    {
        var resolver = context.Services.GetRequiredService<ScheduledMessageStoreResolver>();

        if (!resolver.TryResolve(context, out var scheduledStore))
        {
            throw ThrowHelper.ScheduledDispatchUnsupported(context.Transport.GetType());
        }

        return scheduledStore;
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

file static class Extensions
{
    public static MessageEnvelope WithScheduledTime(this MessageEnvelope envelope, DateTimeOffset scheduledTime)
    {
        return new MessageEnvelope(envelope) { ScheduledTime = scheduledTime };
    }
}
