using System.Collections;
using System.Runtime.CompilerServices;
using static HotChocolate.ExecutableErrorHelper;

namespace HotChocolate;

internal sealed class DefaultEnumerableExecutable(IEnumerable source) : IExecutable
{
    public object Source => source;

    public ValueTask<IList> ToListAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<object?>();

        foreach (var item in source)
        {
            list.Add(item);
        }

        return new ValueTask<IList>(list);
    }

    public async IAsyncEnumerable<object?> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<object?> stream)
        {
            await foreach (var element in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return element;
            }
        }
        else
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }
    }

    public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetEnumerator();

        try
        {
            return enumerator.MoveNext()
                ? new ValueTask<object?>(enumerator.Current)
                : new ValueTask<object?>(default(object?));
        }
        finally
        {
            if(enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetEnumerator();

        try
        {
            if (enumerator.MoveNext())
            {
                var obj = enumerator.Current;

                if(enumerator.MoveNext())
                {
                    throw new GraphQLException(SequenceContainsMoreThanOneElement());
                }

                return new ValueTask<object?>(obj);
            }

            return new ValueTask<object?>(default(object?));
        }
        finally
        {
            if(enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public string Print() => Source.ToString() ?? Source.GetType().FullName ?? Source.GetType().Name;

    public override string ToString() => Print();
}
