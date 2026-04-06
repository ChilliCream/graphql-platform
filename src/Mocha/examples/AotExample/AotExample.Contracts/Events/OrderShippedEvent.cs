namespace AotExample.Contracts.Events;

public sealed class OrderShippedEvent
{
    public required string OrderId { get; init; }

    public required string TrackingNumber { get; init; }
}
