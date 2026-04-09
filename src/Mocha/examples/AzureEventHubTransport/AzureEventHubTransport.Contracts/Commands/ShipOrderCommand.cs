using Mocha.Sagas;

namespace AzureEventHubTransport.Contracts.Commands;

/// <summary>
/// Sent by the order fulfillment saga to the shipping service to ship the order.
/// </summary>
public sealed class ShipOrderCommand : ICorrelatable
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }

    public required string CustomerEmail { get; init; }

    public Guid? CorrelationId { get; init; }
}
