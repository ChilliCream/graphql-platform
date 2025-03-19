using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class TestQueryInterceptor : PagingQueryInterceptor
{
    public List<string> Queries { get; } = new();

    public override void OnBeforeExecute<T>(IQueryable<T> query)
    {
        lock(Queries)
        {
            Queries.Add(query.ToQueryString());
        }
    }
}
