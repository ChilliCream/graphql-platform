using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate;

internal sealed class DefaultAsyncEnumerableExecutable<T>(IAsyncEnumerable<T> source) : Executable<T>
{
    public override object Source => source;

    public override async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        return await enumerator.MoveNextAsync() ? enumerator.Current : default;
    }

    public override async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);

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

    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<T>();

        await foreach (var element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            result.Add(element);
        }

        return result;
    }

    public async override IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return element;
        }
    }
}
