using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.OrderService.Handlers;

public sealed class CheckInventoryRequestHandler(
    ILogger<CheckInventoryRequestHandler> logger)
    : IEventRequestHandler<CheckInventoryRequest, CheckInventoryResponse>
{
    public ValueTask<CheckInventoryResponse> HandleAsync(
        CheckInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var quantityOnHand = Random.Shared.Next(0, 20);
        var isAvailable = quantityOnHand >= request.Quantity;

        logger.LogInformation(
            "Inventory check for {ProductName}: {QuantityOnHand} on hand, requested {Quantity} — {Result}",
            request.ProductName,
            quantityOnHand,
            request.Quantity,
            isAvailable ? "available" : "insufficient");

        return new ValueTask<CheckInventoryResponse>(
            new CheckInventoryResponse
            {
                IsAvailable = isAvailable,
                QuantityOnHand = quantityOnHand
            });
    }
}
