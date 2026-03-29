using Mocha.Middlewares;

namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Dispatch middleware that extracts a routing key from the message using a
/// <see cref="RabbitMQRoutingKeyExtractor"/> stored on the message type's feature collection,
/// and writes it to the dispatch context headers for the terminal to read.
/// </summary>
internal sealed class RabbitMQRoutingKeyMiddleware
{
    /// <summary>
    /// Invokes the middleware, extracting the routing key if an extractor is configured on the message type.
    /// </summary>
    /// <param name="context">The dispatch context.</param>
    /// <param name="next">The next delegate in the dispatch pipeline.</param>
    public ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.MessageType is not null
            && context.Message is not null
            && context.MessageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out var extractor))
        {
            var routingKey = extractor.Extract(context.Message);
            if (routingKey is not null)
            {
                context.Headers.Set(RabbitMQMessageHeaders.RoutingKey, routingKey);
            }
        }

        return next(context);
    }

    private static readonly RabbitMQRoutingKeyMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="DispatchMiddlewareConfiguration"/> that wraps the routing key middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "RabbitMQRoutingKey".</returns>
    public static DispatchMiddlewareConfiguration Create()
        => new(
            static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "RabbitMQRoutingKey");
}
