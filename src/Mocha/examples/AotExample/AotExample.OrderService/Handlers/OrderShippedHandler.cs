using AotExample.Contracts.Events;
using AotExample.OrderService.Notifications;
using Mocha;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class OrderShippedHandler(
    IPublisher publisher,
    ILogger<OrderShippedHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public async ValueTask HandleAsync(
        OrderShippedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} shipped with tracking {TrackingNumber}",
            message.OrderId,
            message.TrackingNumber);

        await publisher.PublishAsync(
            new OrderStatusChangedNotification
            {
                OrderId = message.OrderId,
                Status = "Shipped"
            },
            cancellationToken);
    }
}
