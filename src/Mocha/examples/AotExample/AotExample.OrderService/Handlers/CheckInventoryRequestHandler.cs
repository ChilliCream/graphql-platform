using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.OrderService.Handlers;

public sealed partial class CheckInventoryRequestHandler(
    ILogger<CheckInventoryRequestHandler> logger)
    : IEventRequestHandler<CheckInventoryRequest, CheckInventoryResponse>
{
    public ValueTask<CheckInventoryResponse> HandleAsync(
        CheckInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var quantityOnHand = Random.Shared.Next(0, 20);
        var isAvailable = quantityOnHand >= request.Quantity;

        LogInventoryCheck(
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Inventory check for {ProductName}: {QuantityOnHand} on hand, requested {Quantity} — {Result}")]
    private partial void LogInventoryCheck(string productName, int quantityOnHand, int quantity, string result);
}
