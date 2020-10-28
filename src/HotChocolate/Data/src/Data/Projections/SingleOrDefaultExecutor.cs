using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    internal static class SingleOrDefaultExecutor
    {
        public static async Task<object?> ExecuteAsync<T>(
            IResolverContext? context,
            object? result,
            CancellationToken ct)
        {
            if (result is IAsyncEnumerable<T> ae)
            {
                await using IAsyncEnumerator<T> enumerator = ae.GetAsyncEnumerator(ct);

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    result = enumerator.Current;
                }
                else
                {
                    result = default(T)!;
                }

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    if (context is not null)
                    {
                        result = ErrorHelper.ProjectionProvider_CreateMoreThanOneError(context);
                    }
                    else
                    {
                        result = ErrorHelper.ProjectionProvider_CreateMoreThanOneError();
                    }
                }
            }
            else if (result is IEnumerable<T> e)
            {
                result = await Task
                    .Run<object?>(
                        () =>
                        {
                            try
                            {
                                return e.SingleOrDefault();
                            }
                            catch (InvalidOperationException)
                            {
                                if (context is not null)
                                {
                                    return ErrorHelper.ProjectionProvider_CreateMoreThanOneError(context);
                                }

                                return ErrorHelper.ProjectionProvider_CreateMoreThanOneError();
                            }
                        },
                        ct)
                    .ConfigureAwait(false);
            }

            return result;
        }
    }
}
