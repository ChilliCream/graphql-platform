using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Provides outbox control for a dispatch.
/// </summary>
public static class OutboxDispatchContextExtensions
{
    /// <summary>
    /// Sends the message to the transport instead of the outbox.
    /// </summary>
    /// <param name="context">The dispatch context to modify.</param>
    public static void SkipOutbox(this IDispatchContext context)
    {
        var feature = context.Features.GetOrSet<OutboxMiddlewareFeature>();
        feature.SkipOutbox = true;
    }
}
