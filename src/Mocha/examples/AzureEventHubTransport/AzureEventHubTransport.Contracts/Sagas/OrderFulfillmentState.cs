using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Sagas;

/// <summary>
/// Tracks the state of an order as it moves through the fulfillment pipeline:
/// placed → payment processed → shipped → fulfilled.
/// </summary>
public sealed class OrderFulfillmentState : SagaStateBase
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal TotalAmount { get; init; }

    public required string CustomerEmail { get; init; }

    // Populated after payment
    public Guid? PaymentId { get; set; }

    // Populated after shipping
    public string? TrackingNumber { get; set; }

    public string? Carrier { get; set; }
}
