using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections;

/// <summary>
/// Returns the first element of the sequence that satisfies a condition or a default value if
/// no such element is found.
/// </summary>
public sealed class FirstOrDefaultMiddleware<T>
{
    public const string ContextKey = nameof(FirstOrDefaultMiddleware<object>);

    private readonly FieldDelegate _next;

    public FirstOrDefaultMiddleware(FieldDelegate next)
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
                    // Apply limit.
                    if (ae is IQueryable<T> q)
                    {
                        q = q.Take(1);

                        ae = (IAsyncEnumerable<T>)q;
                    }

                    await using var enumerator =
                        ae.GetAsyncEnumerator(context.RequestAborted);

                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        context.Result = enumerator.Current;
                    }
                    else
                    {
                        context.Result = default(T)!;
                    }

                    break;
                }
            case IEnumerable<T> e:
                context.Result = await Task
                    .Run(() => e.FirstOrDefault(), context.RequestAborted)
                    .ConfigureAwait(false);
                break;
            case IExecutable ex:
                context.Result = await ex
                    .FirstOrDefaultAsync(context.RequestAborted)
                    .ConfigureAwait(false);
                break;
        }
    }
}
