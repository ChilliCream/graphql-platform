namespace AzureEventHubTransport.Contracts.Events;

/// <summary>
/// Published when the order fulfillment saga completes successfully.
/// Payment has been processed and the order has been shipped.
/// </summary>
public sealed class OrderFulfilledEvent
{
    public required Guid OrderId { get; init; }

    public required Guid PaymentId { get; init; }

    public required string TrackingNumber { get; init; }

    public required string Carrier { get; init; }

    public required DateTimeOffset FulfilledAt { get; init; }
}
