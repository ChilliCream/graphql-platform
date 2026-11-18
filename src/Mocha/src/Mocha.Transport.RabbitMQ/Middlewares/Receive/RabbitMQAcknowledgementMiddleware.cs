using Mocha.Middlewares;
using Mocha.Transport.RabbitMQ.Features;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

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
        }
        catch
        {
            await TrySettleAsync(context, channel, eventArgs, ack: false);
            throw;
        }

        // Once next returns the handler's work is committed, so the ack is sent without the shutdown token.
        await TrySettleAsync(context, channel, eventArgs, ack: true);
    }

    private async ValueTask TrySettleAsync(
        IReceiveContext context,
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        bool ack)
    {
        if (channel.IsOpen)
        {
            try
            {
                if (ack)
                {
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, false, CancellationToken.None);
                }
                else
                {
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true, CancellationToken.None);
                }

                return;
            }
            catch (Exception ex) when (ex is AlreadyClosedException or ObjectDisposedException)
            {
                // The channel closed after the check above.
            }
        }

        (context.Transport as RabbitMQMessagingTransport)?.ConsumerManager.Logger.SettlementSkipped(
            context,
            (context.Endpoint as RabbitMQReceiveEndpoint)?.Queue.Name,
            ack ? "ack" : "nack",
            eventArgs);
    }

    /// <summary>
    /// Creates middleware that acknowledges handled deliveries and requeues failed deliveries.
    /// </summary>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (_, next) =>
            {
                var middleware = new RabbitMQAcknowledgementMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "RabbitMQAcknowledgement");
}
