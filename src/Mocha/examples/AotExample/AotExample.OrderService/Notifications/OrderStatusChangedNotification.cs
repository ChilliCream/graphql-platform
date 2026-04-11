using Mocha.Mediator;

namespace AotExample.OrderService.Notifications;

public sealed class OrderStatusChangedNotification : INotification
{
    public required string OrderId { get; init; }

    public required string Status { get; init; }
}
