using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchExecutablePagingContainer<TEntity> : IElasticSearchPagingContainer<TEntity>
{
    private readonly IElasticSearchExecutable<TEntity> _executable;

    private int _take;
    private int _skip;

    public ElasticSearchExecutablePagingContainer(IElasticSearchExecutable<TEntity> executable)
    {
        _executable = executable;
    }

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken) => _executable.CountAsync(cancellationToken);

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteQueryAsync(int offset, CancellationToken cancellationToken)
    {
        _executable.WitPagination(_take, offset);
        var searchResults = await _executable.ExecuteAsync(cancellationToken);
        return searchResults
            .Select((searchResult, i) => IndexEdge<TEntity>.Create(searchResult, offset + i))
            .ToList();
    }

    /// <inheritdoc />
    public async ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
    {
        _executable.WitPagination(_take, _skip);
        var result = await _executable.ExecuteAsync(cancellationToken);
        return result.ToList();
    }

    /// <inheritdoc />
    public IElasticSearchPagingContainer<TEntity> Skip(int skip)
    {
        _skip = skip;
        return this;
    }

    /// <inheritdoc />
    public IElasticSearchPagingContainer<TEntity> Take(int take)
    {
        _take = take;
        return this;
    }
}
