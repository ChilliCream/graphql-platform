using HotChocolate.Types.Pagination;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenPagingContainer<TEntity>
{
    private IAsyncDocumentQuery<TEntity> _query;

    public RavenPagingContainer(IAsyncDocumentQuery<TEntity> query)
    {
        _query = query;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
        => _query.CountAsync(cancellationToken);

    public async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteQueryAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        var list = new List<IndexEdge<TEntity>>();
        await using var cursor =
            await _query.AsAsyncEnumerable(cancellationToken).ConfigureAwait(false);

        var index = offset;
        while (await cursor.MoveNextAsync().ConfigureAwait(false))
        {
            list.Add(IndexEdge<TEntity>.Create(cursor.Current.Document, index++));
        }

        return list;
    }

    public async ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
    {
        return await _query.ToListAsync(cancellationToken);
    }

    public RavenPagingContainer<TEntity> Skip(int skip)
    {
        _query = _query.Skip(skip);

        return this;
    }

    public RavenPagingContainer<TEntity> Take(int take)
    {
        _query = _query.Take(take);

        return this;
    }
}

static file class LocalExtensions
{
    public static Task<IAsyncEnumerator<StreamResult<T>>> AsAsyncEnumerable<T>(
        this IAsyncDocumentQuery<T> self, CancellationToken cancellationToken)
    {
        var ravenQueryProvider = (AsyncDocumentQuery<T>)self;

        return ravenQueryProvider.AsyncSession.Advanced.StreamAsync(self, cancellationToken);
    }
}
