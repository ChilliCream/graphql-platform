using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    public class SingleOrDefaultMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public SingleOrDefaultMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            if (context.Result is ISingleOrDefaultExecutable ae)
            {
                context.Result = ae.AddSingleOrDefault();
            }
            else
            {
                context.Result = await SingleOrDefaultExecutor
                    .ExecuteAsync<T>(context, context.Result, context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
