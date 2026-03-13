using Demo.Billing.Data;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Billing.Handlers;

public class ShipmentShippedEventHandler(BillingDbContext db, ILogger<ShipmentShippedEventHandler> logger)
    : IEventHandler<ShipmentShippedEvent>
{
    public async ValueTask HandleAsync(ShipmentShippedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Shipment {ShipmentId} shipped for order {OrderId}", message.ShipmentId, message.OrderId);

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == message.OrderId, cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice for order {OrderId} not found", message.OrderId);
            return;
        }

        // Log shipping info on the invoice (in a real app you might track this differently)
        logger.LogInformation(
            "Order {OrderId} shipped via {Carrier} with tracking {TrackingNumber}. "
                + "Estimated delivery: {EstimatedDelivery}",
            message.OrderId,
            message.Carrier,
            message.TrackingNumber,
            message.EstimatedDelivery);
    }
}
