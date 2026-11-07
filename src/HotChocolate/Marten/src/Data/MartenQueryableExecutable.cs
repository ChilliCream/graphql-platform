using System.Runtime.CompilerServices;
using Marten;

#pragma warning disable CS8714

namespace HotChocolate.Data.Marten;

internal sealed class MartenQueryableExecutable<T>(IQueryable<T> source)
    : Executable<T>
    , IQueryableExecutable<T>
{
    public override IQueryable<T> Source => source;

    public bool IsInMemory => false;

    public IQueryableExecutable<T> WithSource(IQueryable<T> source)
        => new MartenQueryableExecutable<T>(source);

    public IQueryableExecutable<TQuery> WithSource<TQuery>(IQueryable<TQuery> source)
        => new MartenQueryableExecutable<TQuery>(source);

    public override async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        => await source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        => await source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => await source.CountAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        => [.. await source.ToListAsync(cancellationToken).ConfigureAwait(false)];

    public override async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.ToAsyncEnumerable(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public override string Print()
        => source.ToCommand().CommandText;
}
