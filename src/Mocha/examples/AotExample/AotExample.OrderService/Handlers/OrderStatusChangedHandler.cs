using AotExample.OrderService.Notifications;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed partial class OrderStatusChangedHandler(
    ILogger<OrderStatusChangedHandler> logger)
    : INotificationHandler<OrderStatusChangedNotification>
{
    public ValueTask HandleAsync(
        OrderStatusChangedNotification notification,
        CancellationToken cancellationToken)
    {
        LogOrderStatusChanged(notification.OrderId, notification.Status);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} status changed to {Status}")]
    private partial void LogOrderStatusChanged(string orderId, string status);
}
