using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Middlewares;

/// <summary>
/// Dispatch middleware that runs the per-type extractors for the Azure Service Bus native message
/// properties (<c>SessionId</c>, <c>PartitionKey</c>, <c>ReplyToSessionId</c>, <c>To</c>) and writes
/// the resulting values to the dispatch context headers for the terminal endpoint to read.
/// </summary>
/// <remarks>
/// User-set headers win: each extractor runs only when the corresponding header has not already been
/// set on <see cref="IDispatchContext.Headers"/> by an upstream caller. The
/// <c>PartitionKey</c>/<c>SessionId</c> invariant is enforced in the dispatch endpoint where both
/// final values are known regardless of origin.
/// </remarks>
internal sealed class AzureServiceBusMessagePropertiesMiddleware
{
    /// <summary>
    /// Invokes the middleware, running each extractor whose header has not already been set.
    /// </summary>
    /// <param name="context">The dispatch context.</param>
    /// <param name="next">The next delegate in the dispatch pipeline.</param>
    public ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.MessageType is { } messageType && context.Message is { } message)
        {
            var headers = context.Headers;

            if (!headers.ContainsKey(AzureServiceBusMessageHeaders.SessionId)
                && messageType.Features.TryGet<AzureServiceBusSessionIdExtractor>(out var sessionIdExtractor))
            {
                var sessionId = sessionIdExtractor.Extract(message);
                if (sessionId is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.SessionId, sessionId);
                }
            }

            if (!headers.ContainsKey(AzureServiceBusMessageHeaders.PartitionKey)
                && messageType.Features.TryGet<AzureServiceBusPartitionKeyExtractor>(out var partitionKeyExtractor))
            {
                var partitionKey = partitionKeyExtractor.Extract(message);
                if (partitionKey is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.PartitionKey, partitionKey);
                }
            }

            if (!headers.ContainsKey(AzureServiceBusMessageHeaders.ReplyToSessionId)
                && messageType.Features.TryGet<AzureServiceBusReplyToSessionIdExtractor>(out var replyToSessionIdExtractor))
            {
                var replyToSessionId = replyToSessionIdExtractor.Extract(message);
                if (replyToSessionId is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.ReplyToSessionId, replyToSessionId);
                }
            }

            if (!headers.ContainsKey(AzureServiceBusMessageHeaders.To)
                && messageType.Features.TryGet<AzureServiceBusToExtractor>(out var toExtractor))
            {
                var to = toExtractor.Extract(message);
                if (to is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.To, to);
                }
            }
        }

        return next(context);
    }

    private static readonly AzureServiceBusMessagePropertiesMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="DispatchMiddlewareConfiguration"/> that wraps the message properties middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "AzureServiceBusMessageProperties".</returns>
    public static DispatchMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "AzureServiceBusMessageProperties");
}
