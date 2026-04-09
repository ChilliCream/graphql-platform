using Mocha;
using AzureEventHubTransport.Contracts.Events;

namespace AzureEventHubTransport.NotificationService.Handlers;

public sealed class OrderFulfilledNotificationHandler(ILogger<OrderFulfilledNotificationHandler> logger)
    : IEventHandler<OrderFulfilledEvent>
{
    public ValueTask HandleAsync(OrderFulfilledEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[EMAIL] Order complete summary sent for order {OrderId} — payment {PaymentId}, shipped via {Carrier} ({TrackingNumber})",
            message.OrderId,
            message.PaymentId,
            message.Carrier,
            message.TrackingNumber);

        return ValueTask.CompletedTask;
    }
}
