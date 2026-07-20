using Demo.Contracts.Commands;
using Demo.Shipping.Data;
using Demo.Shipping.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Shipping.Handlers;

public class CreateReturnLabelCommandHandler(ShippingDbContext db, ILogger<CreateReturnLabelCommandHandler> logger)
    : IEventRequestHandler<CreateReturnLabelCommand, CreateReturnLabelResponse>
{
    public async ValueTask<CreateReturnLabelResponse> HandleAsync(
        CreateReturnLabelCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating return label for order {OrderId}, original shipment {ShipmentId}",
            request.OrderId,
            request.OriginalShipmentId);

        // Verify the original shipment exists
        var originalShipment = await db.Shipments.FirstOrDefaultAsync(
            s => s.Id == request.OriginalShipmentId,
            cancellationToken);

        if (originalShipment is null)
        {
            logger.LogWarning("Original shipment {ShipmentId} not found for return", request.OriginalShipmentId);

            return new CreateReturnLabelResponse
            {
                ReturnId = Guid.Empty,
                OrderId = request.OrderId,
                Success = false,
                ReturnTrackingNumber = null,
                ReturnLabelUrl = null,
                FailureReason = $"Original shipment {request.OriginalShipmentId} not found",
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        // Generate return tracking number and label URL
        var trackingNumber = $"RTN-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        var labelUrl = $"https://shipping.example.com/labels/{trackingNumber}.pdf";

        // Create return shipment record
        var returnShipment = new ReturnShipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            OriginalShipmentId = request.OriginalShipmentId,
            CustomerAddress = request.CustomerAddress,
            CustomerId = request.CustomerId,
            TrackingNumber = trackingNumber,
            LabelUrl = labelUrl,
            Status = ReturnShipmentStatus.LabelCreated,
            CreatedAt = DateTimeOffset.UtcNow,
            // Store order details for saga when package arrives
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Amount = request.Amount,
            Reason = request.Reason
        };

        db.ReturnShipments.Add(returnShipment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Return label created: {ReturnId} with tracking {TrackingNumber}",
            returnShipment.Id,
            trackingNumber);

        return new CreateReturnLabelResponse
        {
            ReturnId = returnShipment.Id,
            OrderId = request.OrderId,
            Success = true,
            ReturnTrackingNumber = trackingNumber,
            ReturnLabelUrl = labelUrl,
            FailureReason = null,
            CreatedAt = returnShipment.CreatedAt
        };
    }
}
