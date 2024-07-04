using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static HotChocolate.ExecutableErrorHelper;

namespace HotChocolate;

internal sealed class DefaultQueryableExecutable<T>(IQueryable<T> source, Func<IQueryable<T>, string>? printer = null)
    : Executable<T>
    , IQueryableExecutable<T>
{
    private readonly Func<IQueryable<T>, string> _printer = printer ?? (q => q.ToString() ?? string.Empty);

    public override object Source => source;

    public bool IsInMemory { get; } = source is EnumerableQuery;

    IQueryable<T> IQueryableExecutable<T>.Source => source;

    public IQueryableExecutable<T> WithSource(IQueryable<T> src)
        => new DefaultQueryableExecutable<T>(src);

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
            return await enumerator.MoveNextAsync().ConfigureAwait(false) ? enumerator.Current : default;
        }

#if NET6_0_OR_GREATER
        return await Task.Run(() => source.FirstOrDefault<T>(), cancellationToken).WaitAsync(cancellationToken);
#else
        return await Task.Run(() => source.FirstOrDefault<T>(), cancellationToken);
#endif
    }

    public override ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (source is EnumerableQuery)
        {
            return new ValueTask<T?>(SingleOrDefaultSync(source));
        }

        return SingleOrDefaultFromDataSourceAsync(cancellationToken);
    }

    private async ValueTask<T?> SingleOrDefaultFromDataSourceAsync(CancellationToken cancellationToken = default)
    {
        if (source is IAsyncEnumerable<T> asyncEnumerable)
        {
            await using var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken);
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                var result = enumerator.Current;

                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    throw new GraphQLException(SequenceContainsMoreThanOneElement());
                }

                return result;
            }

            return default;
        }

#if NET6_0_OR_GREATER
        return await Task.Run(() => SingleOrDefaultSync(source), cancellationToken).WaitAsync(cancellationToken);
#else
        return await Task.Run(() => SingleOrDefaultSync(source), cancellationToken);
#endif
    }

    private static T? SingleOrDefaultSync(IQueryable<T> query)
    {
        var enumerator = query.GetEnumerator();

        try
        {
            if (enumerator.MoveNext())
            {
                var obj = enumerator.Current;

                if(enumerator.MoveNext())
                {
                    throw new GraphQLException(SequenceContainsMoreThanOneElement());
                }

                return obj;
            }

            return default;
        }
        finally
        {
            if(enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
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

#if NET6_0_OR_GREATER
        return await Task.Run(() => source.ToList(), cancellationToken).WaitAsync(cancellationToken);
#else
        return await Task.Run(() => source.ToList(), cancellationToken);
#endif
    }

    public override async IAsyncEnumerable<T> ToAsyncEnumerable(
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
#if NET6_0_OR_GREATER
            var list = await Task.Run(() => source.ToList(), cancellationToken).WaitAsync(cancellationToken);
#else
            var list = await Task.Run(() => source.ToList(), cancellationToken);
#endif

            foreach (var element in list)
            {
                yield return element;
            }
        }
    }

    public override string Print() => _printer(source);
}
