namespace AzureServiceBusTransport.Contracts.Events;

public sealed class OrderShippedEvent
{
    public required Guid OrderId { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset ShippedAt { get; init; }
}
