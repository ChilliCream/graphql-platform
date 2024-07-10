using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class FindFluentPagingContainer<TEntity> : IMongoPagingContainer<TEntity>
{
    public readonly IFindFluent<TEntity, TEntity> _source;
    private readonly IFindFluent<TEntity, TEntity> _initSource;

    public FindFluentPagingContainer(IFindFluent<TEntity, TEntity> source)
    {
        // This is the only way to somewhat clone the IFindFluent
        _initSource = source.Project(Builders<TEntity>.Projection.As<TEntity>());
        _source = source;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return (int)await _initSource
            .CountDocumentsAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteQueryAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        var list = new List<IndexEdge<TEntity>>();

        using var cursor = await _source
            .ToCursorAsync(cancellationToken)
            .ConfigureAwait(false);

        var index = offset;
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in cursor.Current)
            {
                list.Add(IndexEdge<TEntity>.Create(item, index++));
            }
        }

        return list;
    }

    public async ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
    {
        return await _source.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public IMongoPagingContainer<TEntity> Skip(int skip)
    {
        return new FindFluentPagingContainer<TEntity>(_source.Skip(skip));
    }

    public IMongoPagingContainer<TEntity> Take(int take)
    {
        return new FindFluentPagingContainer<TEntity>(_source.Limit(take));
    }

    public static FindFluentPagingContainer<TEntity> New(
        IFindFluent<TEntity, TEntity> find) =>
        new FindFluentPagingContainer<TEntity>(find);
}
