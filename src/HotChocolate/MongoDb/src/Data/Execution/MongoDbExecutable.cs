using System.Collections;
using System.Runtime.CompilerServices;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// Is the base class for a executable for the MongoDb.
/// </summary>
public abstract class MongoDbExecutable<T> : IExecutable<T>, IMongoDbExecutable
{
    /// <summary>
    /// The filter definition that was set by <see cref="WithFiltering"/>
    /// </summary>
    protected MongoDbFilterDefinition? Filters { get; private set; }

    /// <summary>
    /// The sort definition that was set by <see cref="WithSorting"/>
    /// </summary>
    protected MongoDbSortDefinition? Sorting { get; private set; }

    /// <summary>
    /// The projection definition that was set by <see cref="WithProjection"/>
    /// </summary>
    protected MongoDbProjectionDefinition? Projection { get; private set; }

    /// <inheritdoc />
    public IMongoDbExecutable WithFiltering(MongoDbFilterDefinition filters)
    {
        Filters = filters;
        return this;
    }

    /// <inheritdoc />
    public IMongoDbExecutable WithSorting(MongoDbSortDefinition sorting)
    {
        Sorting = sorting;
        return this;
    }

    /// <inheritdoc />
    public IMongoDbExecutable WithProjection(MongoDbProjectionDefinition projection)
    {
        Projection = projection;
        return this;
    }

    /// <inheritdoc />
    public abstract object Source { get; }

    /// <inheritdoc />
    async ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => await ToListAsync(cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken);

    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach(var item in ToAsyncEnumerable(cancellationToken))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public abstract IAsyncEnumerable<T> ToAsyncEnumerable(CancellationToken cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    async ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract ValueTask<int> CountAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract string Print();
}
