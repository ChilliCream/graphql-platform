using Demo.Contracts.Events;
using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Mediator;

namespace Demo.Shipping.Commands;

public record ReceiveReturnPackageCommand(Guid ReturnId) : ICommand<ReceiveReturnPackageResult>;

public record ReceiveReturnPackageResult(
    bool Success,
    ReturnShipment? ReturnShipment = null,
    string? Error = null);

public class ReceiveReturnPackageCommandHandler(
    ShippingDbContext db,
    IMessageBus messageBus,
    ILogger<ReceiveReturnPackageCommandHandler> logger)
    : ICommandHandler<ReceiveReturnPackageCommand, ReceiveReturnPackageResult>
{
    public async ValueTask<ReceiveReturnPackageResult> HandleAsync(
        ReceiveReturnPackageCommand command, CancellationToken cancellationToken)
    {
        var returnShipment = await db.ReturnShipments.FirstOrDefaultAsync(
            r => r.Id == command.ReturnId, cancellationToken);
        if (returnShipment is null)
            return new ReceiveReturnPackageResult(false, Error: "Return shipment not found");

        if (returnShipment.Status == ReturnShipmentStatus.Received)
            return new ReceiveReturnPackageResult(false, Error: "Return package already received");

        returnShipment.Status = ReturnShipmentStatus.Received;
        returnShipment.ReceivedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Return package {ReturnId} received, publishing ReturnPackageReceivedEvent",
            returnShipment.Id);

        await messageBus.PublishAsync(
            new ReturnPackageReceivedEvent
            {
                ReturnId = returnShipment.Id,
                OrderId = returnShipment.OrderId,
                TrackingNumber = returnShipment.TrackingNumber!,
                ReceivedAt = returnShipment.ReceivedAt.Value,
                ProductId = returnShipment.ProductId,
                Quantity = returnShipment.Quantity,
                Amount = returnShipment.Amount,
                CustomerId = returnShipment.CustomerId,
                Reason = returnShipment.Reason
            },
            cancellationToken);

        return new ReceiveReturnPackageResult(true, returnShipment);
    }
}
