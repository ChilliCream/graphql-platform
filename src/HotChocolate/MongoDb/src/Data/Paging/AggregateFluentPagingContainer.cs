using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class AggregateFluentPagingContainer<TEntity> : IMongoPagingContainer<TEntity>
{
    private readonly IAggregateFluent<TEntity> _source;
    private readonly IAggregateFluent<AggregateCountResult> _countSource;

    public AggregateFluentPagingContainer(IAggregateFluent<TEntity> source)
    {
        _countSource = source.Count();
        _source = source;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var result = await _countSource
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return (int)(result?.Count ?? 0L);
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
        return new AggregateFluentPagingContainer<TEntity>(_source.Skip(skip));
    }

    public IMongoPagingContainer<TEntity> Take(int take)
    {
        return new AggregateFluentPagingContainer<TEntity>(_source.Limit(take));
    }

    public static AggregateFluentPagingContainer<TEntity> New(
        IAggregateFluent<TEntity> aggregate) => new(aggregate);
}
