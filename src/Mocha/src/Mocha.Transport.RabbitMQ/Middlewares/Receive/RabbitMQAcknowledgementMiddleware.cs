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
        var completedDeliveries = feature.CompletedDeliveries;
        var messageId = eventArgs.BasicProperties.MessageId;

        if (eventArgs.Redelivered && messageId is not null && completedDeliveries?.TryRemove(messageId) == true)
        {
            // The handler completed this message before, on a channel that closed before the delivery was acked.
            GetLogger(context).RedeliveryOfCompletedMessageAcked(messageId, eventArgs.RoutingKey);

            if (!await TryAckAsync(context, channel, eventArgs))
            {
                completedDeliveries.Add(messageId);
            }

            return;
        }

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
        if (!await TryAckAsync(context, channel, eventArgs) && messageId is not null)
        {
            // The broker redelivers the message, which is then acked without running the handler again.
            completedDeliveries?.Add(messageId);
        }
    }

    private static async ValueTask<bool> TryAckAsync(
        IReceiveContext context,
        IChannel channel,
        BasicDeliverEventArgs eventArgs)
    {
        if (channel.IsOpen)
        {
            try
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, CancellationToken.None);
                return true;
            }
            catch (Exception ex) when (ex is AlreadyClosedException or ObjectDisposedException)
            {
                // The channel closed after the check above.
            }
        }

        GetLogger(context).SettlementSkippedChannelClosed("ack", eventArgs.DeliveryTag, eventArgs.RoutingKey);
        return false;
    }

    private static async ValueTask TryNackAsync(
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

        GetLogger(context).SettlementSkippedChannelClosed("nack", eventArgs.DeliveryTag, eventArgs.RoutingKey);
    }

    private static ILogger<RabbitMQAcknowledgementMiddleware> GetLogger(IReceiveContext context)
        => context.Services.GetRequiredService<ILogger<RabbitMQAcknowledgementMiddleware>>();

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

internal static partial class RabbitMQAcknowledgementMiddlewareLogs
{
    [LoggerMessage(
        LogLevel.Information,
        "Acked redelivered message {MessageId} (routing key {RoutingKey}) without handling it again, because its handler already completed")]
    public static partial void RedeliveryOfCompletedMessageAcked(
        this ILogger logger,
        string messageId,
        string routingKey);
}
