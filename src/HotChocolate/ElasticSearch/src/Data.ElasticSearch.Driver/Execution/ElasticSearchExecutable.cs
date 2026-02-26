using System.Collections;
using System.Runtime.CompilerServices;

namespace HotChocolate.Data.ElasticSearch.Execution;

public abstract class ElasticSearchExecutable<T> : IElasticSearchExecutable<T>
{
    protected ISearchOperation? Filters { get; private set; }

    protected IReadOnlyList<ElasticSearchSortOperation>? Sorting { get; private set; }

    protected int? Take { get; private set; }

    protected int? Skip { get; private set; }

    /// <inheritdoc />
    public abstract string Print();

    /// <inheritdoc />
    public abstract object Source { get; }

    /// <inheritdoc />
    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(cancellationToken);
        return result.ToList();
    }

    /// <inheritdoc />
    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in ToAsyncEnumerable(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var result = await ToListAsync(cancellationToken);
        foreach (var item in result)
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
    {
        var result = await ToListAsync(cancellationToken);
        return result.FirstOrDefault();
    }

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
    {
        var result = await ToListAsync(cancellationToken);
        return result.SingleOrDefault();
    }

    /// <inheritdoc />
    public abstract ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<int> CountAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public IElasticSearchExecutable WithFiltering(ISearchOperation filters)
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
