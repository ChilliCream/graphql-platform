using Mocha.Mediator;

namespace AotExample.Contracts.Notifications;

public sealed class OrderStatusChangedNotification : INotification
{
    public required string OrderId { get; init; }

    public required string Status { get; init; }
}
