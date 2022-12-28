using System.Collections;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.ElasticSearch.Execution;

public abstract class ElasticSearchExecutable<T> :
    IElasticSearchExecutable,
    IExecutable<T>
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
    public abstract string GetName(IFilterField field);

    /// <inheritdoc />
    public abstract string GetName(ISortField field);

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
    public IElasticSearchExecutable WitPagination(int take, int skip)
    {
        Take = take;
        Skip = skip;
        return this;
    }
}
