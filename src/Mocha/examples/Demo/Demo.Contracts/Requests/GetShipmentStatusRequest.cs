using Mocha;

namespace Demo.Contracts.Requests;

/// <summary>
/// Request to get shipment status from the Shipping service.
/// </summary>
public sealed class GetShipmentStatusRequest : IEventRequest<GetShipmentStatusResponse>
{
    public required Guid OrderId { get; init; }
}
