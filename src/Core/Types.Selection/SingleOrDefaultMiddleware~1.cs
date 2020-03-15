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

                switch (context.Result)
                {
                    case IAsyncEnumerable<T> ae:
                        bool found = false;
                        await foreach (T result in ae.ConfigureAwait(false))
                        {
                            if (found)
                            {
                                throw new InvalidOperationException(
                                    "Sequence contains more than one element");
                            }
                            found = true;
                            context.Result = result;
                            if (options.AllowMultipleResults)
                            {
                                break;
                            }
                        }
                        break;
                    case IEnumerable<T> e:
                        if (options.AllowMultipleResults)
                        {
                            context.Result = await Task.Run(
                                    () => e.FirstOrDefault(), context.RequestAborted)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            context.Result = await Task.Run(
                                    () => e.SingleOrDefault(), context.RequestAborted)
                                .ConfigureAwait(false);
                        }
                        break;
                }
            }
        }
    }
}
