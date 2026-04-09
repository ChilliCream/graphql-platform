using Mocha;
using KafkaTransport.Contracts.Events;

namespace KafkaTransport.NotificationService.Handlers;

public sealed class OrderPlacedNotificationHandler(ILogger<OrderPlacedNotificationHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending order confirmation to {CustomerEmail} for order {OrderId}: {Quantity}x {ProductName} (${TotalAmount})",
            message.CustomerEmail,
            message.OrderId,
            message.Quantity,
            message.ProductName,
            message.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
