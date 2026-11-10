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
internal sealed class RabbitMQAcknowledgementMiddleware(ILogger<RabbitMQAcknowledgementMiddleware> logger)
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
            await TryNackAsync(context, channel, eventArgs);
            throw;
        }

        // Once next returns the handler's work is committed, so the ack is sent without the shutdown token.
        await TryAckAsync(context, channel, eventArgs);
    }

    private async ValueTask TryAckAsync(
        IReceiveContext context,
        IChannel channel,
        BasicDeliverEventArgs eventArgs)
    {
        if (channel.IsOpen)
        {
            try
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, CancellationToken.None);
                return;
            }
            catch (Exception ex) when (ex is AlreadyClosedException or ObjectDisposedException)
            {
                // The channel closed after the check above.
            }
        }

        LogSkippedSettlement(context, channel, eventArgs, "ack");
    }

    private async ValueTask TryNackAsync(
        IReceiveContext context,
        IChannel channel,
        BasicDeliverEventArgs eventArgs)
    {
        if (channel.IsOpen)
        {
            try
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true, CancellationToken.None);
                return;
            }
            catch (Exception ex) when (ex is AlreadyClosedException or ObjectDisposedException)
            {
                // The channel closed after the check above.
            }
        }

        LogSkippedSettlement(context, channel, eventArgs, "nack");
    }

    private void LogSkippedSettlement(
        IReceiveContext context,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        string settlement)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        using var connectionScope = (context.Transport as RabbitMQMessagingTransport)?.ConsumerManager.BeginLogScope(channel);
        using var messageScope = logger.BeginMessageScope(context);
        logger.SettlementSkippedChannelClosed(
            settlement,
            delivery.BasicProperties.MessageId ?? context.MessageId,
            delivery.ConsumerTag,
            delivery.DeliveryTag,
            delivery.Exchange,
            delivery.RoutingKey,
            delivery.Redelivered);
    }

    /// <summary>
    /// Creates middleware that acknowledges handled deliveries and requeues failed deliveries.
    /// </summary>
    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var middleware = new RabbitMQAcknowledgementMiddleware(
                    context.Services.GetRequiredService<ILogger<RabbitMQAcknowledgementMiddleware>>());
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "RabbitMQAcknowledgement");
}

internal static partial class RabbitMQAcknowledgementLogs
{
    [LoggerMessage(
        LogLevel.Debug,
        "Skipped {Settlement} of message {MessageId}, consumer {ConsumerTag}, delivery {DeliveryTag} "
            + "from exchange {Exchange} with routing key {RoutingKey} (redelivered: {Redelivered}) because its channel closed")]
    public static partial void SettlementSkippedChannelClosed(
        this ILogger logger,
        string settlement,
        string? messageId,
        string consumerTag,
        ulong deliveryTag,
        string exchange,
        string routingKey,
        bool redelivered);
}
