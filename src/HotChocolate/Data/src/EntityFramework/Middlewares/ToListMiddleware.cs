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

            if (context.Result is IQueryable<TEntity> queryable)
            {
                context.Result = await queryable
                    .ToListAsync(context.RequestAborted)
                    .ConfigureAwait(false);
            }
            else if (context.Result is IExecutable executable)
            {
                context.Result = await executable.ExecuteAsync(context.RequestAborted);
            }
        }
    }
}
