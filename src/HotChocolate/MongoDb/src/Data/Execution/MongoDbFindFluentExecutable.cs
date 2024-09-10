using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// A executable that is based on <see cref="IFindFluent{TInput,TResult}"/>
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class MongoDbFindFluentExecutable<T>(IFindFluent<T, T> findFluent) : MongoDbExecutable<T>
{
    /// <inheritdoc />
    public override object Source => findFluent;

    /// <inheritdoc />
    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken) =>
        await BuildPipeline()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

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
    public override async ValueTask<T?> FirstOrDefaultAsync(
        CancellationToken cancellationToken) =>
        await BuildPipeline()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public override async ValueTask<T?> SingleOrDefaultAsync(
        CancellationToken cancellationToken) =>
        await BuildPipeline()
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public override async ValueTask<int> CountAsync(
        CancellationToken cancellationToken) =>
        (int)await findFluent.CountDocumentsAsync(cancellationToken);

    /// <inheritdoc />
    public override string Print() => BuildPipeline().ToString() ?? "";

    /// <summary>
    /// Applies filtering sorting and projections on the <see cref="IExecutable.Source"/>
    /// </summary>
    /// <returns>A find fluent including the configuration of this executable</returns>
    public virtual IFindFluent<T, T> BuildPipeline()
    {
        var pipeline = findFluent;

        if (Filters is not null)
        {
            pipeline.Filter =
                new AndFilterDefinition(findFluent.Filter.Wrap(), Filters)
                    .ToFilterDefinition<T>();
        }

        if (Sorting is not null)
        {
            if (pipeline.Options?.Sort is { } sortDefinition)
            {
                pipeline.Sort(
                    new MongoDbCombinedSortDefinition(sortDefinition.Wrap(), Sorting)
                        .ToSortDefinition<T>());
            }
            else
            {
                pipeline = pipeline.Sort(Sorting.ToSortDefinition<T>());
            }
        }

        if (Projection is not null)
        {
            pipeline = pipeline.Project<T>(Projection.ToProjectionDefinition<T>());
        }

        return pipeline;
    }
}
