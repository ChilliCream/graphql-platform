using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
internal sealed class RabbitMQAcknowledgementMiddleware(ILogger logger)
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

        LogSkippedSettlement(context, eventArgs, ack ? "ack" : "nack");
    }

    private void LogSkippedSettlement(IReceiveContext context, BasicDeliverEventArgs delivery, string settlement)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        using ((context.Transport as RabbitMQMessagingTransport)?.ConsumerManager.Connection.BeginLogScope())
        using (logger.BeginMessageScope(context))
        {
            logger.SettlementSkipped(
                (context.Endpoint as RabbitMQReceiveEndpoint)?.Queue.Name,
                settlement,
                delivery.BasicProperties.MessageId ?? context.MessageId,
                delivery.ConsumerTag,
                delivery.DeliveryTag,
                delivery.Redelivered);
        }
    }

    /// <summary>
    /// Creates middleware that acknowledges handled deliveries and requeues failed deliveries.
    /// </summary>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var middleware = new RabbitMQAcknowledgementMiddleware(
                    context.Services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(RabbitMQMessagingTransport.ConsumerLogCategory));
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "RabbitMQAcknowledgement");
}
