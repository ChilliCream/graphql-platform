using Mocha.Sagas;

namespace AotExample.Contracts.Events;

public sealed class OrderShippedEvent : ICorrelatable
{
    public required string OrderId { get; init; }

    public required string TrackingNumber { get; init; }

    public Guid? CorrelationId { get; init; }
}
