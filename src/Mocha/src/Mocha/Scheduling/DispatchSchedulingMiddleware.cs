using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Dispatch middleware that intercepts outgoing messages with a scheduled time and persists them
/// to the scheduled message store instead of forwarding them to the next pipeline stage.
/// </summary>
/// <remarks>
/// Messages without a <see cref="IDispatchContext.ScheduledTime"/> pass through to the next middleware.
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

        if (context.Envelope is null)
        {
            await next(context);
            return;
        }

        if (context.Envelope.ScheduledTime != scheduledTime)
        {
            context.Envelope = context.Envelope.WithScheduledTime(scheduledTime);
        }

        var resolver = context.Services.GetRequiredService<ScheduledMessageStoreResolver>();

        if (!resolver.TryResolve(context, out var store))
        {
            throw ThrowHelper.ScheduledDispatchUnsupported(context.Transport.GetType());
        }

        var token = await store.PersistAsync(context, context.CancellationToken);

        context.Features.Configure<ScheduledMessageFeature>(f => f.Token = token);
    }

    internal static DispatchDelegate Create(DispatchDelegate next)
    {
        var middleware = new DispatchSchedulingMiddleware();
        return context => middleware.InvokeAsync(context, next);
    }
}

file static class Extensions
{
    public static MessageEnvelope WithScheduledTime(this MessageEnvelope envelope, DateTimeOffset scheduledTime)
    {
        return new MessageEnvelope(envelope) { ScheduledTime = scheduledTime };
    }
}
