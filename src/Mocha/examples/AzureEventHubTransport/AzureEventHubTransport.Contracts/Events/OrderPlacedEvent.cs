using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Events;

/// <summary>
/// Published when a new order is placed. Initiates the order fulfillment saga.
/// </summary>
public sealed class OrderPlacedEvent
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal TotalAmount { get; init; }

    public required string CustomerEmail { get; init; }

    public required DateTimeOffset PlacedAt { get; init; }
}
