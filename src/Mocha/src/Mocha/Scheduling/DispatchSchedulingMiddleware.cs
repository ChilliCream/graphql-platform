using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Scheduling;

/// <summary>
/// Persists scheduled messages in the scheduled message store of the transport for later delivery.
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
        if (context.ScheduledTime is not { } scheduledTime)
        {
            await next(context);
            return;
        }

        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.EnvelopeNotSet();
        }

        if (envelope.ScheduledTime != scheduledTime)
        {
            context.Envelope = envelope.WithScheduledTime(scheduledTime);
        }

        var resolver = context.Services.GetRequiredService<ScheduledMessageStoreResolver>();

        if (!resolver.TryResolve(context, out var store))
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

file static class Extensions
{
    public static MessageEnvelope WithScheduledTime(this MessageEnvelope envelope, DateTimeOffset scheduledTime)
    {
        return new MessageEnvelope(envelope) { ScheduledTime = scheduledTime };
    }
}
