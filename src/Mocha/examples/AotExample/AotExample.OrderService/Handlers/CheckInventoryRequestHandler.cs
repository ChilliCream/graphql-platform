using AotExample.Contracts.Requests;
using Mocha;

namespace AotExample.OrderService.Handlers;

public sealed class CheckInventoryRequestHandler(ILogger<CheckInventoryRequestHandler> logger)
    : IEventRequestHandler<CheckInventoryRequest, CheckInventoryResponse>
{
    public ValueTask<CheckInventoryResponse> HandleAsync(
        CheckInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var quantityOnHand = Random.Shared.Next(0, 20);
        var isAvailable = quantityOnHand >= request.Quantity;

        logger.LogInventoryCheck(
            request.ProductName,
            quantityOnHand,
            request.Quantity,
            isAvailable ? "available" : "insufficient");

        return new ValueTask<CheckInventoryResponse>(
            new CheckInventoryResponse { IsAvailable = isAvailable, QuantityOnHand = quantityOnHand });
    }
}

internal static partial class Logs
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Inventory check for {ProductName}: {QuantityOnHand} on hand, requested {Quantity} — {Result}")]
    public static partial void LogInventoryCheck(
        this ILogger logger,
        string productName,
        int quantityOnHand,
        int quantity,
        string result);
}
