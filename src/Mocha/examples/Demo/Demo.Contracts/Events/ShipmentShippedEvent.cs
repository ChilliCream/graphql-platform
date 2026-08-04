namespace Demo.Contracts.Events;

/// <summary>
/// Published by Shipping when a shipment is dispatched.
/// </summary>
public sealed class ShipmentShippedEvent
{
    public required Guid ShipmentId { get; init; }
    public required Guid OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required DateTimeOffset ShippedAt { get; init; }
    public required DateTimeOffset EstimatedDelivery { get; init; }
}
