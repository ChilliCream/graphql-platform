using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Selections;

namespace HotChocolate.Types
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
            if (context.Field.ContextData[nameof(SingleOrDefaultOptions)] is
                    SingleOrDefaultOptions options)
            {
                await _next(context).ConfigureAwait(false);

                context.Result = context.Result switch
                {
                    IAsyncEnumerable<T> ae => HandleAsyncEnumerable(options, ae),
                    IEnumerable<T> ae => HandleEnumerable(context, options, ae),
                    _ => context.Result
                };
            }
        }

        private async Task<T> HandleEnumerable(
            IMiddlewareContext context,
            SingleOrDefaultOptions options,
            IEnumerable<T> e)
        {
            return await Task.Run(
                    () => options.AllowMultipleResults ? e.FirstOrDefault() : e.SingleOrDefault(),
                    context.RequestAborted)
                .ConfigureAwait(false);
        }

        private async Task<T> HandleAsyncEnumerable(
            SingleOrDefaultOptions options,
            IAsyncEnumerable<T> ae)
        {
            T returnValue = default;
            await foreach (T result in ae.ConfigureAwait(false))
            {
                if (!Equals(returnValue, default(T)))
                {
                    throw new InvalidOperationException(
                        "Sequence contains more than one element");
                }
                returnValue = result;
                if (options.AllowMultipleResults)
                {
                    break;
                }
            }
            return returnValue;
        }
    }
}
