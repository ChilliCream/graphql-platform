using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Events;

/// <summary>
/// Published by the payment handler when a payment is completed.
/// Correlates back to the order fulfillment saga.
/// </summary>
public sealed class PaymentProcessedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required Guid PaymentId { get; init; }

    public required bool Success { get; init; }

    public required DateTimeOffset ProcessedAt { get; init; }

    public Guid? CorrelationId { get; init; }
}
