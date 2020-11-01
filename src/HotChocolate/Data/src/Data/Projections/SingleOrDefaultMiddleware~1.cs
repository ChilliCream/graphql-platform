using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using static HotChocolate.Data.ErrorHelper;

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

            switch (context.Result)
            {
                case IAsyncEnumerable<T> ae:
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

                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        context.Result = ProjectionProvider_CreateMoreThanOneError(context);
                    }

                    break;
                }
                case IEnumerable<T> e:
                    context.Result = await Task
                        .Run<object?>(
                            () =>
                            {
                                try
                                {
                                    return e.SingleOrDefault();
                                }
                                catch (InvalidOperationException)
                                {
                                    return ProjectionProvider_CreateMoreThanOneError(context);
                                }
                            },
                            context.RequestAborted)
                        .ConfigureAwait(false);
                    break;
                case IExecutable ex:
                    context.Result = await ex.SingleOrDefaultAsync(context.RequestAborted);
                    break;
            }
        }
    }
}
