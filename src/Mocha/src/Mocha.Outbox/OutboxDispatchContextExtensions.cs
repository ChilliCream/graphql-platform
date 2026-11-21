using Mocha.Middlewares;

namespace Mocha.Outbox;

/// <summary>
/// Provides outbox control for a dispatch.
/// </summary>
public static class OutboxDispatchContextExtensions
{
    /// <summary>
    /// Sends the message to the transport instead of the outbox.
    /// The call has no effect for a message published or sent through a DbContext outbox.
    /// </summary>
    /// <param name="context">The dispatch context to modify.</param>
    public static void SkipOutbox(this IDispatchContext context)
    {
        var feature = context.Features.GetOrSet<OutboxMiddlewareFeature>();
        feature.SkipOutbox = true;
    }
}
