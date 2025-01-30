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
        return await context.Brands
            .Where(t => ids.Contains(t.Id))
            .With(query)
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }
}
