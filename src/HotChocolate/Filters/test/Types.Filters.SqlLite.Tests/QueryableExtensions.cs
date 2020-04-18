using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types.Filters
{
    public static class IQueryableExtensions
    {

        public static IObjectFieldDescriptor MatchSql<TEntity>(
            this IObjectFieldDescriptor descriptor) where TEntity : class
        {
            descriptor.Use(next => async context =>
            {
                await next(context);
                if (context.Result is IQueryable<TEntity> result)
                {
                    context.Service<MatchSqlHelper>().Sql = result.ToQueryString();
                }
            });

            return descriptor;
        }
    }
}
