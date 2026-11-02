using Demo.Catalog.Data;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class RestockInventoryCommandHandler(CatalogDbContext db, ILogger<RestockInventoryCommandHandler> logger)
    : IEventRequestHandler<RestockInventoryCommand, RestockInventoryResponse>
{
    public async ValueTask<RestockInventoryResponse> HandleAsync(
        RestockInventoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Restocking {Quantity} units of product {ProductId} from return {ReturnId}",
            request.Quantity,
            request.ProductId,
            request.ReturnId);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found for restock", request.ProductId);
            return new RestockInventoryResponse
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                QuantityRestocked = 0,
                NewStockLevel = 0,
                Success = false,
                FailureReason = $"Product {request.ProductId} not found",
                RestockedAt = DateTimeOffset.UtcNow
            };
        }

        var previousStock = product.StockQuantity;
        product.StockQuantity += request.Quantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Restocked product {ProductId}: {Previous} → {New} (+{Added})",
            request.ProductId,
            previousStock,
            product.StockQuantity,
            request.Quantity);

        return new RestockInventoryResponse
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            QuantityRestocked = request.Quantity,
            NewStockLevel = product.StockQuantity,
            Success = true,
            FailureReason = null,
            RestockedAt = DateTimeOffset.UtcNow
        };
    }
}
