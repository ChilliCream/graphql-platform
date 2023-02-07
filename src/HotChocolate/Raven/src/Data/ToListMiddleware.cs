using HotChocolate.Resolvers;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace HotChocolate.Data.Raven;

internal sealed class ToListMiddleware<TEntity>
{
    private readonly FieldDelegate _next;

    public ToListMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (context.Result is IRavenQueryable<TEntity> queryable)
        {
            context.Result = await queryable
                .Customize(x => x.NoTracking())
                .ToArrayAsync(context.RequestAborted);
        }
    }
}
