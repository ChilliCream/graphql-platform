using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;

namespace Demo.Catalog.Queries;

public record GetCategoriesQuery : IQuery<List<Category>>;

public class GetCategoriesQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetCategoriesQuery, List<Category>>
{
    public async ValueTask<List<Category>> HandleAsync(
        GetCategoriesQuery query, CancellationToken cancellationToken)
        => await db.Categories.ToListAsync(cancellationToken);
}
