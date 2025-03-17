using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Types.SingleProperties;

[QueryType]
public static partial class SinglePropertyQueries
{
    public static async Task<IReadOnlyList<SingleProperty>> GetSingleProperties(
        CatalogContext context,
        QueryContext<SingleProperty> query,
        CancellationToken cancellationToken)
    {
        var queryable = context.SingleProperties.With(query);
        PagingQueryInterceptor.Publish(queryable);
        return await queryable.Take(2).ToListAsync(cancellationToken);
    }
}
