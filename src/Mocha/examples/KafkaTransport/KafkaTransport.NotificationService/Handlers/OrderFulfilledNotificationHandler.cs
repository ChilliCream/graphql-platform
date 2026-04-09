using Mocha;
using KafkaTransport.Contracts.Events;

namespace KafkaTransport.NotificationService.Handlers;

public sealed class OrderFulfilledNotificationHandler(ILogger<OrderFulfilledNotificationHandler> logger)
    : IEventHandler<OrderFulfilledEvent>
{
    public ValueTask HandleAsync(OrderFulfilledEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} fulfilled! {ProductName} shipped via {Carrier} (tracking: {TrackingNumber}). Placed: {PlacedAt}, Shipped: {ShippedAt}, Fulfilled: {FulfilledAt}",
            message.OrderId,
            message.ProductName,
            message.Carrier,
            message.TrackingNumber,
            message.PlacedAt,
            message.ShippedAt,
            message.FulfilledAt);

        return ValueTask.CompletedTask;
    }
}
