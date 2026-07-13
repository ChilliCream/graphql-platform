using AotExample.Contracts.Events;
using AotExample.OrderService.Notifications;
using Mocha;
using Mocha.Mediator;

namespace AotExample.OrderService.Handlers;

public sealed class OrderShippedHandler(IPublisher publisher, ILogger<OrderShippedHandler> logger)
    : IEventHandler<OrderShippedEvent>
{
    public async ValueTask HandleAsync(OrderShippedEvent message, CancellationToken cancellationToken)
    {
        logger.LogOrderShipped(message.OrderId, message.TrackingNumber);

        await publisher.PublishAsync(
            new OrderStatusChangedNotification { OrderId = message.OrderId, Status = "Shipped" },
            cancellationToken);
    }
}

internal static partial class Logs
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} shipped with tracking {TrackingNumber}")]
    public static partial void LogOrderShipped(this ILogger logger, string orderId, string trackingNumber);
}
