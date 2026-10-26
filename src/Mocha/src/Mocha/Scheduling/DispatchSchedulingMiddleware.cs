using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Persists scheduled messages for later delivery.
/// Unscheduled messages pass through to the next middleware.
/// </summary>
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
        var store = (context as DispatchContext)?.MessageStore;
        var requestedTime = context.ScheduledTime ?? (store is not null ? context.Envelope?.ScheduledTime : null);
        if (requestedTime is not { } scheduledTime)
        {
            await next(context);
            return;
        }

        context.ScheduledTime = scheduledTime;
        DispatchSerializerMiddleware.Serialize(context);

        if (context.Envelope!.ScheduledTime != scheduledTime)
        {
            context.Envelope = context.Envelope.WithScheduledTime(scheduledTime);
        }

        var scheduledStore = ResolveStore(context);

        if (store is not null)
        {
            await store.PersistAsync(context);
            return;
        }

        var token = await scheduledStore.PersistAsync(context, context.CancellationToken);

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
