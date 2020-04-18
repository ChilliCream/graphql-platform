using MongoDB.Driver.Linq;

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
                if (context.Result is IMongoQueryable<TEntity> result)
                {
                    context.Service<MatchSqlHelper>().Query = result.ToString();
                }
            });

            return descriptor;
        }
    }
}
