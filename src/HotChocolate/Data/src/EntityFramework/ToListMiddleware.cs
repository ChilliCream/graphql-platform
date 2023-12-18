using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

#pragma warning disable CA1812

internal sealed class ToListMiddleware<TEntity>(FieldDelegate next)
{
    public async ValueTask InvokeAsync(IMiddlewareContext context)
    {
        await next(context).ConfigureAwait(false);

        context.Result = context.Result switch
        {
            IQueryable<TEntity> queryable and IAsyncEnumerable<TEntity> =>
                await queryable
                    .ToListAsync(context.RequestAborted)
                    .ConfigureAwait(false),
            _ => context.Result
        };
    }
}
