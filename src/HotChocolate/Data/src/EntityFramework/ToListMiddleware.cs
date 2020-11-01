using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    internal class ToListMiddleware<TEntity>
    {
        private readonly FieldDelegate _next;

        public ToListMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public async ValueTask InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            context.Result = context.Result switch
            {
                IQueryable<TEntity> queryable =>
                    await queryable
                        .ToListAsync(context.RequestAborted)
                        .ConfigureAwait(false),
                IExecutable<TEntity> executable =>
                    await executable
                        .ToListAsync(context.RequestAborted)
                        .ConfigureAwait(false),
                _ => context.Result
            };
        }
    }
}
