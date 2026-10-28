using HotChocolate.Resolvers;
using Marten.Linq;

namespace HotChocolate.Data.Marten;

internal sealed class MartenQueryBuilder<T>(IQueryBuilder inner) : IQueryBuilder
{
    public void Prepare(IMiddlewareContext context)
        => inner.Prepare(context);

    public void Apply(IMiddlewareContext context)
    {
        context.Result = context.Result switch
        {
            IMartenQueryable<T> queryable => new MartenQueryableExecutable<T>(queryable),
            IQueryableExecutable<T> { Source: IMartenQueryable<T> queryable }
                => new MartenQueryableExecutable<T>(queryable),
            _ => context.Result
        };

        inner.Apply(context);
    }
}
