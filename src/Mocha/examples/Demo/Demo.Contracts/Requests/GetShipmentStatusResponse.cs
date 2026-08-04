namespace Demo.Contracts.Requests;

/// <summary>
/// Response containing shipment status from the Shipping service.
/// </summary>
public sealed class GetShipmentStatusResponse
{
    public required Guid ShipmentId { get; init; }
    public required Guid OrderId { get; init; }
    public required string Status { get; init; }
    public required string? TrackingNumber { get; init; }
    public required string? Carrier { get; init; }
    public required DateTimeOffset? EstimatedDelivery { get; init; }
    public required bool Found { get; init; }
}
