using AotExample.OrderService.Notifications;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class OrderStatusChangedHandler(ILogger<OrderStatusChangedHandler> logger)
    : INotificationHandler<OrderStatusChangedNotification>
{
    public ValueTask HandleAsync(OrderStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogOrderStatusChanged(notification.OrderId, notification.Status);

        return ValueTask.CompletedTask;
    }
}

internal static partial class Logs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} status changed to {Status}")]
    public static partial void LogOrderStatusChanged(this ILogger logger, string orderId, string status);
}
