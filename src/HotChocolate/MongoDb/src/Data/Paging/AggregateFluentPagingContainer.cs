using System.Collections.Immutable;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

internal class AggregateFluentPagingContainer<TEntity>(IAggregateFluent<TEntity> source)
    : IMongoPagingContainer<TEntity>
{
    private readonly IAggregateFluent<AggregateCountResult> _countSource = source.Count();

    public async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        var result = await _countSource
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return (int)(result?.Count ?? 0L);
    }

    public async Task<ImmutableArray<Edge<TEntity>>> QueryAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        using var cursor = await source.ToCursorAsync(cancellationToken).ConfigureAwait(false);

        var index = offset;
        var builder = ImmutableArray.CreateBuilder<Edge<TEntity>>();

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var item in cursor.Current)
            {
                builder.Add(IndexEdge<TEntity>.Create(item, index++));
            }
        }

        return builder.ToImmutable();
    }

    public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
    {
        return await source.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public IMongoPagingContainer<TEntity> Skip(int skip)
    {
        return new AggregateFluentPagingContainer<TEntity>(source.Skip(skip));
    }

    public IMongoPagingContainer<TEntity> Take(int take)
    {
        return new AggregateFluentPagingContainer<TEntity>(source.Limit(take));
    }

    public static AggregateFluentPagingContainer<TEntity> New(
        IAggregateFluent<TEntity> aggregate) => new(aggregate);
}
