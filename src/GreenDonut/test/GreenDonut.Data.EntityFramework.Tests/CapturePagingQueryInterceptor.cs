using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

public sealed class CapturePagingQueryInterceptor : PagingQueryInterceptor
{
    public List<QueryInfo> Queries { get; } = [];

    public override void OnBeforeExecute<T>(IQueryable<T> query)
    {
        Queries.Add(
            new QueryInfo
            {
                ExpressionText = query.Expression.ToString(),
                QueryText = query.ToQueryString()
            });
    }
}
