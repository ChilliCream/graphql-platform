namespace Demo.Contracts.Events;

/// <summary>
/// Published by Shipping when a new shipment is created.
/// </summary>
public sealed class ShipmentCreatedEvent
{
    public required Guid ShipmentId { get; init; }
    public required Guid OrderId { get; init; }
    public required string Address { get; init; }
    public required string TrackingNumber { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
