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

            headers.SetIfExtracted<AzureServiceBusSessionIdExtractor>(
                messageType,
                message,
                AzureServiceBusMessageHeaders.SessionId,
                static (extractor, message) => extractor.Extract(message));

            headers.SetIfExtracted<AzureServiceBusPartitionKeyExtractor>(
                messageType,
                message,
                AzureServiceBusMessageHeaders.PartitionKey,
                static (extractor, message) => extractor.Extract(message));

            headers.SetIfExtracted<AzureServiceBusReplyToSessionIdExtractor>(
                messageType,
                message,
                AzureServiceBusMessageHeaders.ReplyToSessionId,
                static (extractor, message) => extractor.Extract(message));

            headers.SetIfExtracted<AzureServiceBusToExtractor>(
                messageType,
                message,
                AzureServiceBusMessageHeaders.To,
                static (extractor, message) => extractor.Extract(message));
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

file static class Extensions
{
    public static void SetIfExtracted<TExtractor>(
        this IHeaders headers,
        MessageType messageType,
        object message,
        string headerKey,
        Func<TExtractor, object, string?> extract)
    {
        if (headers.ContainsKey(headerKey) || !messageType.Features.TryGet<TExtractor>(out var extractor))
        {
            return;
        }

        var value = extract(extractor, message);
        if (value is not null)
        {
            headers.Set(headerKey, value);
        }
    }
}
