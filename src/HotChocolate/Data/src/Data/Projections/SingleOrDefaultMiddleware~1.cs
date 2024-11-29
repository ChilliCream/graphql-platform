using HotChocolate.Resolvers;
using static HotChocolate.Data.DataResources;

namespace HotChocolate.Data.Projections;

/// <summary>
/// Returns the only element of the resolved value, or a default value if the sequence is empty.
/// </summary>
public class SingleOrDefaultMiddleware<T>(FieldDelegate next)
{
    private readonly FieldDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        await _next(context).ConfigureAwait(false);

        switch (context.Result)
        {
            case IAsyncEnumerable<T> ae:
            {
                // Apply limit.
                if (ae is IQueryable<T> q)
                {
                    q = q.Take(2);

                    ae = (IAsyncEnumerable<T>)q;
                }

                await using var enumerator = ae.GetAsyncEnumerator(context.RequestAborted);

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
                    throw new InvalidOperationException(ProjectionProvider_CreateMoreThanOneError);
                }
                break;
            }
            case IEnumerable<T> e:
                context.Result = await Task.Run<object?>(
                        () => e.SingleOrDefault(),
                        context.RequestAborted)
                    .ConfigureAwait(false);
                break;
            case IExecutable ex:
                context.Result = await ex
                    .SingleOrDefaultAsync(context.RequestAborted)
                    .ConfigureAwait(false);
                break;
        }
    }
}
