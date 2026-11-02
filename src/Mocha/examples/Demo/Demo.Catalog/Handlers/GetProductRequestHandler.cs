using Demo.Catalog.Data;
using Demo.Contracts.Requests;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Catalog.Handlers;

public class GetProductRequestHandler(CatalogDbContext db, ILogger<GetProductRequestHandler> logger)
    : IEventRequestHandler<GetProductRequest, GetProductResponse>
{
    public async ValueTask<GetProductResponse> HandleAsync(
        GetProductRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting product details for {ProductId}", request.ProductId);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found", request.ProductId);
            return new GetProductResponse
            {
                ProductId = request.ProductId,
                Name = string.Empty,
                Description = string.Empty,
                Price = 0,
                StockQuantity = 0,
                IsAvailable = false
            };
        }

        return new GetProductResponse
        {
            ProductId = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsAvailable = product.StockQuantity > 0
        };
    }
}
