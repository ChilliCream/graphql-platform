using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    public sealed class FirstOrDefaultMiddleware<T>
    {
        private readonly FieldDelegate _next;

        public FirstOrDefaultMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context).ConfigureAwait(false);

            if (context.Result is IAsyncEnumerable<T> ae)
            {
                await using IAsyncEnumerator<T> enumerator =
                    ae.GetAsyncEnumerator(context.RequestAborted);

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    context.Result = enumerator.Current;
                }
                else
                {
                    context.Result = default(T)!;
                }
            }
            else if (context.Result is IEnumerable<T> e)
            {
                context.Result = await Task.Run(
                    () => e.FirstOrDefault(), context.RequestAborted)
                    .ConfigureAwait(false);
            }
        }
    }
}
