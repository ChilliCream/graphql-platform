namespace AzureServiceBusTransport.Contracts.Commands;

public sealed class ShipmentPreparedResponse
{
    public required Guid OrderId { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset PreparedAt { get; init; }
}
