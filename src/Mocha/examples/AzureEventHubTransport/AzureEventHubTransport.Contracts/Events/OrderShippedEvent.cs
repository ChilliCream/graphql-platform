using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Events;

/// <summary>
/// Published by the shipping service when an order is dispatched.
/// Correlates back to the order fulfillment saga.
/// </summary>
public sealed class OrderShippedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset ShippedAt { get; init; }

    public Guid? CorrelationId { get; init; }
}
