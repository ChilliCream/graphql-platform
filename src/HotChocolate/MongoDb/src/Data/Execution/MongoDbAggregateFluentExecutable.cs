using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// A executable that is based on <see cref="IAggregateFluent{TInput}"/>
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class MongoDbAggregateFluentExecutable<T>(IAggregateFluent<T> aggregate) : MongoDbExecutable<T>
{
    /// <inheritdoc />
    public override object Source => aggregate;

    /// <inheritdoc />
    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => await BuildPipeline().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var cursor = await BuildPipeline().ToCursorAsync(cancellationToken).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    /// <inheritdoc />
    public override async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => await BuildPipeline().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public override async ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => await BuildPipeline().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public override async ValueTask<int> CountAsync(CancellationToken cancellationToken)
    {
        var item = await aggregate.Count().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return (int)(item?.Count ?? 0);
    }

    /// <inheritdoc />
    public override string Print() => BuildPipeline().ToString() ?? "";

    /// <summary>
    /// Applies filtering sorting and projections on the <see cref="IExecutable.Source"/>
    /// </summary>
    /// <returns>A aggregate fluent including the configuration of this executable</returns>
    public virtual IAggregateFluent<T> BuildPipeline()
    {
        var pipeline = aggregate;
        if (Sorting is not null)
        {
            pipeline = pipeline.Sort(Sorting.ToSortDefinition<T>());
        }

        if (Filters is not null)
        {
            pipeline = pipeline.Match(Filters.ToFilterDefinition<T>());
        }

        if (Projection is not null)
        {
            pipeline = pipeline.Project<T>(Projection.ToProjectionDefinition<T>());
        }

        return pipeline;
    }
}
