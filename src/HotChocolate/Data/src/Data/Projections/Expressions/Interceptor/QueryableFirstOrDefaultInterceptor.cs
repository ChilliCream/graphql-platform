// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableFirstOrDefaultInterceptor : QueryableTakeHandlerInterceptor
{
    public QueryableFirstOrDefaultInterceptor()
        : base(SelectionFlags.FirstOrDefault, 1)
    {
    }
}
