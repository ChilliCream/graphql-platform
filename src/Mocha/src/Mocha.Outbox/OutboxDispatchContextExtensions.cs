using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Provides convenience methods on <see cref="IDispatchContext"/> for outbox control.
/// </summary>
public static class OutboxDispatchContextExtensions
{
    /// <summary>
    /// Marks the current dispatch context to bypass the outbox, causing the message to be sent directly to the transport.
    /// </summary>
    /// <param name="context">The dispatch context to modify.</param>
    public static void SkipOutbox(this IDispatchContext context)
    {
        var feature = context.Features.GetOrSet<OutboxMiddlewareFeature>();
        feature.SkipOutbox = true;
    }
}
