using System.Collections;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Execution;

public abstract class ElasticSearchExecutable<T> :
    IElasticSearchExecutable<T>
{
    protected BoolOperation? Filters { get; private set; }

    protected IReadOnlyList<ElasticSearchSortOperation>? Sorting { get; private set; }

    protected int? Take { get; private set; }

    protected int? Skip { get; private set; }

    /// <inheritdoc />
    public abstract ValueTask<IList> ToListAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);
    /// <inheritdoc />
    public abstract ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract string Print();

    /// <inheritdoc />
    public abstract object Source { get; }

    /// <inheritdoc />
    public IElasticSearchExecutable WithFiltering(BoolOperation filters)
    {
        Filters = filters;
        return this;
    }

    /// <inheritdoc />
    public IElasticSearchExecutable WithSorting(IReadOnlyList<ElasticSearchSortOperation> sorting)
    {
        Sorting = sorting;
        return this;
    }

    /// <inheritdoc />
    public abstract Task<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<int> CountAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public IElasticSearchExecutable<T> WithTake(int take)
    {
        Take = take;
        return this;
    }

    /// <inheritdoc />
    public IElasticSearchExecutable<T> WithSkip(int skip)
    {
        Skip = skip;
        return this;
    }
}
