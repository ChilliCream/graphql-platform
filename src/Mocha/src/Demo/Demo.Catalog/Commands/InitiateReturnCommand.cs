using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.Mediator;

namespace Demo.Catalog.Commands;

public record InitiateReturnCommand(
    Guid OrderId,
    Guid ShipmentId,
    string Reason) : ICommand<InitiateReturnResult>;

public record InitiateReturnResult(
    bool Success,
    Guid? ReturnId = null,
    string? ReturnTrackingNumber = null,
    string? ReturnLabelUrl = null,
    string? Error = null);

public class InitiateReturnCommandHandler(
    CatalogDbContext db,
    IMessageBus messageBus,
    ILogger<InitiateReturnCommandHandler> logger)
    : ICommandHandler<InitiateReturnCommand, InitiateReturnResult>
{
    public async ValueTask<InitiateReturnResult> HandleAsync(
        InitiateReturnCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);
        if (order is null)
        {
            return new InitiateReturnResult(false, Error: "Order not found");
        }

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Shipping)
        {
            return new InitiateReturnResult(false, Error: $"Order cannot be returned in status: {order.Status}");
        }

        logger.LogInformation("Creating return label for order {OrderId}", command.OrderId);

        try
        {
            var labelResponse = await messageBus.RequestAsync(
                new CreateReturnLabelCommand
                {
                    OrderId = command.OrderId,
                    OriginalShipmentId = command.ShipmentId,
                    CustomerAddress = order.ShippingAddress,
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    Amount = order.TotalAmount,
                    Reason = command.Reason
                },
                cancellationToken);

            if (!labelResponse.Success)
            {
                return new InitiateReturnResult(false, Error: $"Failed to create return label: {labelResponse.FailureReason}");
            }

            order.Status = OrderStatus.ReturnInitiated;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Return label created for order {OrderId}: {ReturnId}, tracking: {Tracking}",
                command.OrderId, labelResponse.ReturnId, labelResponse.ReturnTrackingNumber);

            return new InitiateReturnResult(
                true,
                labelResponse.ReturnId,
                labelResponse.ReturnTrackingNumber,
                labelResponse.ReturnLabelUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create return label for order {OrderId}", command.OrderId);
            return new InitiateReturnResult(false, Error: "Failed to create return label: " + ex.Message);
        }
    }
}
