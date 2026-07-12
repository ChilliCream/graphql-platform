using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Contracts.Saga;
using Mocha;
using Mocha.Mediator;

namespace Demo.Catalog.Commands;

public record RequestQuickRefundCommand(
    Guid OrderId,
    decimal? Amount,
    string Reason) : ICommand<RequestQuickRefundResult>;

public record RequestQuickRefundResult(
    bool Success,
    QuickRefundResponse? Response = null,
    string? Error = null);

public class RequestQuickRefundCommandHandler(
    CatalogDbContext db,
    IMessageBus messageBus,
    ILogger<RequestQuickRefundCommandHandler> logger)
    : ICommandHandler<RequestQuickRefundCommand, RequestQuickRefundResult>
{
    public async ValueTask<RequestQuickRefundResult> HandleAsync(
        RequestQuickRefundCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.OrderId], cancellationToken);
        if (order is null)
        {
            return new RequestQuickRefundResult(false, Error: "Order not found");
        }

        logger.LogInformation("Initiating quick refund saga for order {OrderId}", command.OrderId);

        try
        {
            var response = await messageBus.RequestAsync(
                new RequestQuickRefundRequest
                {
                    OrderId = command.OrderId,
                    Amount = command.Amount ?? order.TotalAmount,
                    CustomerId = order.CustomerId,
                    Reason = command.Reason
                },
                cancellationToken);

            if (response.Success)
            {
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return new RequestQuickRefundResult(true, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quick refund saga failed for order {OrderId}", command.OrderId);
            return new RequestQuickRefundResult(false, Error: "Refund processing failed: " + ex.Message);
        }
    }
}
