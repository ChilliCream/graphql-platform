using AotExample.Contracts.Events;
using AotExample.OrderService.Notifications;
using Mocha;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed partial class OrderShippedHandler(
    IPublisher publisher,
    ILogger<OrderShippedHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public async ValueTask HandleAsync(
        OrderShippedEvent message,
        CancellationToken cancellationToken)
    {
        LogOrderShipped(message.OrderId, message.TrackingNumber);

        await publisher.PublishAsync(
            new OrderStatusChangedNotification
            {
                OrderId = message.OrderId,
                Status = "Shipped"
            },
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} shipped with tracking {TrackingNumber}")]
    private partial void LogOrderShipped(string orderId, string trackingNumber);
}
