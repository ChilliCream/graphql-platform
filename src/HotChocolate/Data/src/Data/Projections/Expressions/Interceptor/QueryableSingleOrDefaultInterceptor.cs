// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableSingleOrDefaultInterceptor
    : QueryableTakeHandlerInterceptor
{
    public QueryableSingleOrDefaultInterceptor()
        : base(SelectionFlags.SingleOrDefault, 2)
    {
    }

    public static QueryableSingleOrDefaultInterceptor Create(ProjectionProviderContext context) => new();
}
