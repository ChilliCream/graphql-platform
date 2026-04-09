using Mocha.Sagas;

namespace KafkaTransport.Contracts.Events;

public sealed class OrderShippedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset ShippedAt { get; init; }

    Guid? ICorrelatable.CorrelationId => OrderId;
}
