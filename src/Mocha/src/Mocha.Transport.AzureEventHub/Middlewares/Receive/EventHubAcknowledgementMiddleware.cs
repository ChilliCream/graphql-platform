using Mocha.Middlewares;

namespace Mocha.Transport.AzureEventHub.Middlewares;

/// <summary>
/// Receive middleware for Event Hub acknowledgement. Event Hubs does not have per-message ack;
/// checkpoint-based progress tracking is handled by the <see cref="MochaEventProcessor"/>.
/// This middleware is a pass-through that ensures the pipeline structure matches other transports.
/// </summary>
internal sealed class EventHubAcknowledgementMiddleware
{
    /// <summary>
    /// Invokes the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The receive context containing the current message.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        // Event Hubs does not have per-message ack.
        // Process the message. If it throws, the exception propagates.
        // Checkpoint-based progress tracking is handled by the MochaEventProcessor.
        await next(context);
    }

    private static readonly EventHubAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "EventHubAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "EventHubAcknowledgement");
}
