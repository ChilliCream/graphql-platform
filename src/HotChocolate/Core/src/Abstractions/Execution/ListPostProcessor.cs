using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                return await executable.ToListAsync(cancellationToken);

            case IExecutable executable:
                return await executable.ToListAsync(cancellationToken);

            default:
                return result;
        }
    }

    public IAsyncEnumerable<object?> ToStreamResult(object result, CancellationToken cancellationToken)
    {
        if(result is IAsyncEnumerable<object?> asyncEnumerable)
        {
            return asyncEnumerable;
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

    public static ListPostProcessor<T> Default { get; } = new();
}
