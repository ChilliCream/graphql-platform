using System.Collections.Immutable;
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

    public async Task<ImmutableArray<Edge<TEntity>>> QueryAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        using var cursor = await _source.ToCursorAsync(cancellationToken).ConfigureAwait(false);

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
        => await _source.ToListAsync(cancellationToken).ConfigureAwait(false);

    public IMongoPagingContainer<TEntity> Skip(int skip)
        => new FindFluentPagingContainer<TEntity>(_source.Skip(skip));

    public IMongoPagingContainer<TEntity> Take(int take)
        => new FindFluentPagingContainer<TEntity>(_source.Limit(take));

    public static FindFluentPagingContainer<TEntity> New(
        IFindFluent<TEntity, TEntity> find)
        => new(find);
}
