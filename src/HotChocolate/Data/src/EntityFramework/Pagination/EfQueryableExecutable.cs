using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Pagination;

internal sealed class EfQueryableExecutable<T>(IQueryable<T> source)
    : Executable<T>
    , IQueryableExecutable<T>
{
    public override IQueryable<T> Source => source;

    public bool IsInMemory => source is EnumerableQuery;

    public IQueryableExecutable<T> WithSource(IQueryable<T> src)
        => new EfQueryableExecutable<T>(src);

    public IQueryableExecutable<TQuery> WithSource<TQuery>(IQueryable<TQuery> src)
        => new EfQueryableExecutable<TQuery>(src);

    public override async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach(var item in source.AsAsyncEnumerable()
            .WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public override async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        => await source.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        => await source.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => await source.CountAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken = default)
        => await source.ToListAsync(cancellationToken).ConfigureAwait(false);

    public override string Print()
        => source.ToQueryString();
}
