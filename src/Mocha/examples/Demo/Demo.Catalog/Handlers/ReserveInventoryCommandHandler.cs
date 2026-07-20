using Demo.Catalog.Data;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class ReserveInventoryCommandHandler(CatalogDbContext db, ILogger<ReserveInventoryCommandHandler> logger)
    : IEventRequestHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(ReserveInventoryCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Reserving {Quantity} units of product {ProductId} for order {OrderId}",
            request.Quantity,
            request.ProductId,
            request.OrderId);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found", request.ProductId);
            throw new InvalidOperationException($"Product {request.ProductId} not found");
        }

        if (product.StockQuantity < request.Quantity)
        {
            logger.LogWarning(
                "Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}",
                request.ProductId,
                product.StockQuantity,
                request.Quantity);
            throw new InvalidOperationException($"Insufficient stock for product {request.ProductId}");
        }

        product.StockQuantity -= request.Quantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reserved {Quantity} units of product {ProductId}. Remaining stock: {Remaining}",
            request.Quantity,
            request.ProductId,
            product.StockQuantity);
    }
}
