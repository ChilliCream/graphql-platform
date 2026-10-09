using Mocha.Mediator;

namespace AzureServiceBusTransport.OrderService.Mediator.Notifications;

/// <summary>
/// In-process notification for order activity auditing.
/// Multiple handlers can subscribe to this notification.
/// </summary>
public sealed class OrderActivityNotification : INotification
{
    public required Guid OrderId { get; init; }

    public required string Activity { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
