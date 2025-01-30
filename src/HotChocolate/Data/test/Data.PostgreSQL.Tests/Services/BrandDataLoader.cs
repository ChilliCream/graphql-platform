using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Services;

internal static class BrandDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> ids,
        QueryContext<Brand> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        var queryable = context.Brands
            .Where(t => ids.Contains(t.Id))
            .With(query);
        PagingQueryInterceptor.Publish(queryable);
        return await queryable.ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
