using Mocha.Sagas;

namespace AotExample.Contracts.Events;

public sealed class OrderPlacedEvent : ICorrelatable
{
    public required string OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public Guid? CorrelationId { get; init; }
}
