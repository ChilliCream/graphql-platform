using Mocha;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.NotificationService.Handlers;

public sealed class OrderPlacedNotificationHandler(ILogger<OrderPlacedNotificationHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[EMAIL] Order confirmation sent to {CustomerEmail} — order {OrderId}: {Quantity}x {ProductName} (${TotalAmount})",
            message.CustomerEmail,
            message.OrderId,
            message.Quantity,
            message.ProductName,
            message.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
