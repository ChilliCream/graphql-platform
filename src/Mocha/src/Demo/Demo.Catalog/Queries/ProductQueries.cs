using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Catalog.Queries;

public record GetProductsQuery : IQuery<List<Product>>;

public class GetProductsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductsQuery, List<Product>>
{
    public async ValueTask<List<Product>> HandleAsync(
        GetProductsQuery query, CancellationToken cancellationToken)
        => await db.Products.Include(p => p.Category).ToListAsync(cancellationToken);
}

public record GetProductByIdQuery(Guid Id) : IQuery<Product?>;

public class GetProductByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetProductByIdQuery, Product?>
{
    public async ValueTask<Product?> HandleAsync(
        GetProductByIdQuery query, CancellationToken cancellationToken)
        => await db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);
}
