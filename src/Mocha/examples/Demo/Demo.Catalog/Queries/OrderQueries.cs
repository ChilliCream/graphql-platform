using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Catalog.Queries;

public record GetOrdersQuery : IQuery<List<OrderRecord>>;

public class GetOrdersQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetOrdersQuery, List<OrderRecord>>
{
    public async ValueTask<List<OrderRecord>> HandleAsync(
        GetOrdersQuery query, CancellationToken cancellationToken)
        => await db.Orders.Include(o => o.Product).ToListAsync(cancellationToken);
}

public record GetOrderByIdQuery(Guid Id) : IQuery<OrderRecord?>;

public class GetOrderByIdQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetOrderByIdQuery, OrderRecord?>
{
    public async ValueTask<OrderRecord?> HandleAsync(
        GetOrderByIdQuery query, CancellationToken cancellationToken)
        => await db.Orders.Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == query.Id, cancellationToken);
}
