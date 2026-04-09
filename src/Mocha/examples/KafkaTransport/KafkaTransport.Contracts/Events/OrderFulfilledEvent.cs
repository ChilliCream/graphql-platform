namespace KafkaTransport.Contracts.Events;

public sealed class OrderFulfilledEvent
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset PlacedAt { get; init; }

    public required DateTimeOffset ShippedAt { get; init; }

    public required DateTimeOffset FulfilledAt { get; init; }
}
