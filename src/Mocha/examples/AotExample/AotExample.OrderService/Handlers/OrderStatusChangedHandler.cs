using AotExample.Contracts.Notifications;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class OrderStatusChangedHandler(
    ILogger<OrderStatusChangedHandler> logger)
    : INotificationHandler<OrderStatusChangedNotification>
{
    public ValueTask HandleAsync(
        OrderStatusChangedNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} status changed to {Status}",
            notification.OrderId,
            notification.Status);

        return ValueTask.CompletedTask;
    }
}
