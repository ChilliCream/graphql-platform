using Mocha;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.NotificationService.Handlers;

public sealed class OrderShippedNotificationHandler(ILogger<OrderShippedNotificationHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public ValueTask HandleAsync(OrderShippedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[EMAIL] Shipment notification sent for order {OrderId} — {Carrier} tracking {TrackingNumber}",
            message.OrderId,
            message.Carrier,
            message.TrackingNumber);

        return ValueTask.CompletedTask;
    }
}
