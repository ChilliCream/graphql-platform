using Mocha;
using KafkaTransport.Contracts.Events;

namespace KafkaTransport.NotificationService.Handlers;

public sealed class OrderShippedNotificationHandler(ILogger<OrderShippedNotificationHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public ValueTask HandleAsync(OrderShippedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending shipment notification for order {OrderId}: shipped via {Carrier}, tracking {TrackingNumber}",
            message.OrderId,
            message.Carrier,
            message.TrackingNumber);

        return ValueTask.CompletedTask;
    }
}
