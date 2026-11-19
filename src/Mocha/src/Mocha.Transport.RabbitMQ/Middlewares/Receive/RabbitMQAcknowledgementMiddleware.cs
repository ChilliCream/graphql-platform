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
/// <param name="logger">The logger for a settlement that was skipped.</param>
/// <param name="queueName">The name of the queue the endpoint consumes.</param>
internal sealed class RabbitMQAcknowledgementMiddleware(RabbitMQConsumerLogger logger, string queueName)
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

        logger.SettlementSkipped(context, queueName, ack, eventArgs);
    }

    /// <summary>
    /// Creates middleware that acknowledges handled deliveries and requeues failed deliveries.
    /// </summary>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var transport = (RabbitMQMessagingTransport)context.Transport;
                var endpoint = (RabbitMQReceiveEndpoint)context.Endpoint;
                var middleware = new RabbitMQAcknowledgementMiddleware(
                    transport.ConsumerManager.Logger,
                    endpoint.Queue.Name);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "RabbitMQAcknowledgement");
}
