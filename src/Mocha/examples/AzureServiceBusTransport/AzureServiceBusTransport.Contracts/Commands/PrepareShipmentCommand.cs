using Mocha;

namespace AzureServiceBusTransport.Contracts.Commands;

/// <summary>
/// Sent by the order fulfillment saga to the shipping service to prepare a shipment.
/// The saga waits for a <see cref="ShipmentPreparedResponse"/> before proceeding.
/// </summary>
public sealed class PrepareShipmentCommand : IEventRequest<ShipmentPreparedResponse>
{
    public required Guid OrderId { get; init; }

    public required string ProductName { get; init; }

    public required int Quantity { get; init; }
}
