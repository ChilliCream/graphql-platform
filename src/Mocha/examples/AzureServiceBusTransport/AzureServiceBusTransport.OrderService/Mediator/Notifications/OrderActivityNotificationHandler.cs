using Mocha.Mediator;

namespace AzureServiceBusTransport.OrderService.Mediator.Notifications;

public sealed class OrderActivityNotificationHandler(
    ILogger<OrderActivityNotificationHandler> logger)
    : INotificationHandler<OrderActivityNotification>
{
    public ValueTask HandleAsync(
        OrderActivityNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Audit: {Activity} for order {OrderId} at {Timestamp}",
            notification.Activity,
            notification.OrderId,
            notification.Timestamp);

        return ValueTask.CompletedTask;
    }
}
