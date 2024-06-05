using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate;

internal sealed class DefaultQueryableExecutable<T>(IQueryable<T> source) : Executable<T>
{
    public override object Source => source;

    public override ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (source is EnumerableQuery)
        {
            return new ValueTask<T?>(source.FirstOrDefault());
        }

        return FirstOrDefaultFromDataSourceAsync(cancellationToken);
    }

    private async ValueTask<T?> FirstOrDefaultFromDataSourceAsync(CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<T> asyncEnumerable)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            return await enumerator.MoveNextAsync() ? enumerator.Current : default;
        }

        return await Task.Run(() => source.FirstOrDefault<T>(), cancellationToken).WaitAsync(cancellationToken);
    }

    public override ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (source is EnumerableQuery)
        {
            return new ValueTask<T?>(source.SingleOrDefault());
        }

        return SingleOrDefaultFromDataSourceAsync(cancellationToken);
    }

    private async ValueTask<T?> SingleOrDefaultFromDataSourceAsync(CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<T> asyncEnumerable)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync())
            {
                var result = enumerator.Current;

                if (await enumerator.MoveNextAsync())
                {
                    throw new InvalidOperationException("Sequence contains more than one element.");
                }

                return result;
            }

            return default;
        }

        return await Task.Run(() => source.SingleOrDefault<T>(), cancellationToken).WaitAsync(cancellationToken);
    }

    public override ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        if (source is EnumerableQuery)
        {
            return new ValueTask<List<T>>(source.ToList());
        }

        return ToListFromDataSourceAsync(cancellationToken);
    }

    private async ValueTask<List<T>> ToListFromDataSourceAsync(CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<T> asyncEnumerable)
        {
            var result = new List<T>();

            await foreach (var element in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                result.Add(element);
            }

            return result;
        }

        return await Task.Run(() => source.ToList(), cancellationToken).WaitAsync(cancellationToken);
    }

    public async override IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<T> asyncEnumerable)
        {
            await foreach (var element in asyncEnumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return element;
            }
        }
        else if (source is EnumerableQuery)
        {
            foreach (var element in source)
            {
                yield return element;
            }
        }
        else
        {
            var list = await Task.Run(() => source.ToList(), cancellationToken).WaitAsync(cancellationToken);

            foreach (var element in list)
            {
                yield return element;
            }
        }
    }
}
