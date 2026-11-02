using Demo.Contracts.Requests;
using Demo.Shipping.Data;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Shipping.Handlers;

public class GetShipmentStatusRequestHandler(ShippingDbContext db, ILogger<GetShipmentStatusRequestHandler> logger)
    : IEventRequestHandler<GetShipmentStatusRequest, GetShipmentStatusResponse>
{
    public async ValueTask<GetShipmentStatusResponse> HandleAsync(
        GetShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting shipment status for order {OrderId}", request.OrderId);

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);

        if (shipment is null)
        {
            logger.LogWarning("Shipment for order {OrderId} not found", request.OrderId);
            return new GetShipmentStatusResponse
            {
                ShipmentId = Guid.Empty,
                OrderId = request.OrderId,
                Status = "NotFound",
                TrackingNumber = null,
                Carrier = null,
                EstimatedDelivery = null,
                Found = false
            };
        }

        return new GetShipmentStatusResponse
        {
            ShipmentId = shipment.Id,
            OrderId = shipment.OrderId,
            Status = shipment.Status.ToString(),
            TrackingNumber = shipment.TrackingNumber,
            Carrier = shipment.Carrier,
            EstimatedDelivery = shipment.EstimatedDelivery,
            Found = true
        };
    }
}
