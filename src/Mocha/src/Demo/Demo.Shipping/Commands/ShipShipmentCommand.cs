using Demo.Contracts.Events;
using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.Mediator;

namespace Demo.Shipping.Commands;

public record ShipShipmentCommand(Guid ShipmentId, string Carrier, int EstimatedDays = 5)
    : ICommand<ShipShipmentResult>;

public record ShipShipmentResult(bool Success, Shipment? Shipment = null, string? Error = null);

public class ShipShipmentCommandHandler(ShippingDbContext db, IMessageBus messageBus)
    : ICommandHandler<ShipShipmentCommand, ShipShipmentResult>
{
    public async ValueTask<ShipShipmentResult> HandleAsync(
        ShipShipmentCommand command, CancellationToken cancellationToken)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(
            s => s.Id == command.ShipmentId, cancellationToken);
        if (shipment is null)
            return new ShipShipmentResult(false, Error: "Shipment not found");

        if (shipment.Status == ShipmentStatus.Shipped)
            return new ShipShipmentResult(false, Error: "Shipment already shipped");

        shipment.Status = ShipmentStatus.Shipped;
        shipment.Carrier = command.Carrier;
        shipment.ShippedAt = DateTimeOffset.UtcNow;
        shipment.EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(command.EstimatedDays);
        await db.SaveChangesAsync(cancellationToken);

        await messageBus.PublishAsync(
            new ShipmentShippedEvent
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                TrackingNumber = shipment.TrackingNumber!,
                Carrier = shipment.Carrier,
                ShippedAt = shipment.ShippedAt.Value,
                EstimatedDelivery = shipment.EstimatedDelivery.Value
            },
            cancellationToken);

        return new ShipShipmentResult(true, shipment);
    }
}
