using System.Collections;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution;

/// <summary>
/// A post processor that can be used to post process async list
/// results like async enumerables, queryables or executables.
/// </summary>
/// <typeparam name="T">
/// The type of the elements in the list.
/// </typeparam>
public sealed class ListPostProcessor<T> : IResolverResultPostProcessor
{
    public async ValueTask<object?> ToCompletionResultAsync(object result, CancellationToken cancellationToken)
    {
        switch (result)
        {
            case IAsyncEnumerable<T> asyncEnumerable:
                return await Executable.From(asyncEnumerable).ToListAsync(cancellationToken);

            case IQueryable<T> queryable:
                return await Executable.From(queryable).ToListAsync(cancellationToken);

            case IExecutable<T> executable:
                try
                {
                    return await executable.ToListAsync(cancellationToken);
                }
                finally
                {
                    if (result is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (result is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

            case IExecutable executable:
                try
                {
                    return await executable.ToListAsync(cancellationToken);
                }
                finally
                {
                    if (result is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (result is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

            default:
                return result;
        }
    }

    public IAsyncEnumerable<object?> ToStreamResultAsync(object result, CancellationToken cancellationToken)
    {
        if(result is IAsyncEnumerable<object?> asyncEnumerable)
        {
            return asyncEnumerable;
        }

        if (result is IExecutable executable
            && result is IDisposable or IAsyncDisposable)
        {
            return DisposableStream(executable, result, cancellationToken);
        }

        return ToExecutable(result).ToAsyncEnumerable(cancellationToken);
    }

    private static IExecutable ToExecutable(object result)
    {
        switch (result)
        {
            case IAsyncEnumerable<T> asyncEnumerable:
                return Executable.From(asyncEnumerable);

            case IQueryable<T> queryable:
                return Executable.From(queryable);

            case IExecutable executable:
                return executable;

            case IEnumerable enumerable:
                return Executable.From(enumerable);

            default:
                throw new NotSupportedException(
                    "The result type is not supported by the list post processor.");
        }
    }

    private static async IAsyncEnumerable<object?> DisposableStream(
        IExecutable executable,
        object result,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            await foreach(var item in executable.ToAsyncEnumerable(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            switch (result)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;

                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    public static ListPostProcessor<T> Default { get; } = new();
}
