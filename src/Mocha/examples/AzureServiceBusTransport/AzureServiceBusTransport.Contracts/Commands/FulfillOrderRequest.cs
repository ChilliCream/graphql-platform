using Mocha;

namespace AzureServiceBusTransport.Contracts.Commands;

/// <summary>
/// Initiates the order fulfillment saga which orchestrates shipment preparation
/// across services before returning a final result.
/// </summary>
public sealed class FulfillOrderRequest : IEventRequest<FulfillOrderResponse>
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required decimal TotalAmount { get; init; }

    public required string CustomerEmail { get; init; }
}
