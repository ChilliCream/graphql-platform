using Mocha.Sagas;

namespace KafkaTransport.Contracts.Events;

public sealed class OrderPlacedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal TotalAmount { get; init; }

    public required string CustomerEmail { get; init; }

    public required DateTimeOffset PlacedAt { get; init; }

    Guid? ICorrelatable.CorrelationId => OrderId;
}
