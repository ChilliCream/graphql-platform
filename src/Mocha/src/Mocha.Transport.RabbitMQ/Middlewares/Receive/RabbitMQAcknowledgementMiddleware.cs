using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;

namespace Mocha.Transport.RabbitMQ.Middlewares;

/// <summary>
/// Receive middleware that sends a BasicAck on successful processing and a BasicNack (with requeue) on failure,
/// ensuring messages are properly acknowledged or returned to the broker.
/// </summary>
internal sealed class RabbitMQAcknowledgementMiddleware
{
    /// <summary>
    /// Invokes the next middleware in the pipeline and acknowledges or negatively acknowledges the message based on the outcome.
    /// </summary>
    /// <param name="context">The receive context containing the current message and features.</param>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<RabbitMQReceiveFeature>();
        var channel = feature.Channel;
        var eventArgs = feature.EventArgs;

        try
        {
            await next(context);

            // Once next returns the handler's work is committed, so the ack is sent without the
            // shutdown token. A cancelled handler throws and is nacked with requeue instead.
            if (channel.IsOpen)
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, CancellationToken.None);
            }
            else
            {
                LogSettlementSkipped(context, "ack", eventArgs.RoutingKey, eventArgs.DeliveryTag);
            }
        }
        catch
        {
            if (channel.IsOpen)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true, CancellationToken.None);
            }
            else
            {
                LogSettlementSkipped(context, "nack", eventArgs.RoutingKey, eventArgs.DeliveryTag);
            }

            throw;
        }
    }

    private static void LogSettlementSkipped(
        IReceiveContext context,
        string settlement,
        string routingKey,
        ulong deliveryTag)
    {
        var logger = context.Services.GetRequiredService<ILogger<RabbitMQAcknowledgementMiddleware>>();
        logger.SettlementSkippedChannelClosed(settlement, deliveryTag, routingKey);
    }

    private static readonly RabbitMQAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Creates a <see cref="ReceiveMiddlewareConfiguration"/> that wraps the acknowledgement middleware singleton.
    /// </summary>
    /// <returns>A middleware configuration keyed as "RabbitMQAcknowledgement".</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "RabbitMQAcknowledgement");
}
