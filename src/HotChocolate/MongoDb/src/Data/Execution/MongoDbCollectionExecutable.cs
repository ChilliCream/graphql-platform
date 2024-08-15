using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// An executable that is based on <see cref="IMongoCollection{TResult}"/>
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class MongoDbCollectionExecutable<T> : MongoDbExecutable<T>
{
    private readonly IMongoCollection<T> _collection;

    /// <inheritdoc />
    public MongoDbCollectionExecutable(IMongoCollection<T> collection)
    {
        _collection = collection;
    }

    /// <inheritdoc />
    public override object Source => _collection;

    /// <summary>
    /// The options that were set by <see cref="WithOptions"/>
    /// </summary>
    protected FindOptionsBase? Options { get; private set; }

    /// <summary>
    /// Applies the options to the executable
    /// </summary>
    /// <param name="options">The options</param>
    /// <returns>An executable that contains the options</returns>
    public IMongoDbExecutable WithOptions(FindOptionsBase options)
    {
        Options = options;
        return this;
    }

    /// <inheritdoc />
    public override async ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
    {
        var serializers = _collection.Settings.SerializerRegistry;
        IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

        var options = Options as FindOptions<T> ?? new FindOptions<T>();
        var filters = new BsonDocument();

        if (Sorting is not null)
        {
            options.Sort = Sorting.Render(bsonSerializer, serializers);
        }

        if (Projection is not null)
        {
            options.Projection = Projection.Render(bsonSerializer, serializers);
        }

        if (Filters is not null)
        {
            filters = Filters.Render(bsonSerializer, serializers);
        }

        var cursor = await _collection
            .FindAsync(filters, options, cancellationToken)
            .ConfigureAwait(false);

        return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var serializers = _collection.Settings.SerializerRegistry;
        IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

        var options = Options as FindOptions<T> ?? new FindOptions<T>();
        var filters = new BsonDocument();

        if (Sorting is not null)
        {
            options.Sort = Sorting.Render(bsonSerializer, serializers);
        }

        if (Projection is not null)
        {
            options.Projection = Projection.Render(bsonSerializer, serializers);
        }

        if (Filters is not null)
        {
            filters = Filters.Render(bsonSerializer, serializers);
        }

        var cursor = await _collection
            .FindAsync(filters, options, cancellationToken)
            .ConfigureAwait(false);

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

    public override async ValueTask<int> CountAsync(CancellationToken cancellationToken)
        => (int)await BuildPipeline().CountDocumentsAsync(cancellationToken);

    /// <inheritdoc />
    public override string Print() => BuildPipeline().ToString() ?? "";

    /// <summary>
    /// Applies filtering sorting and projections on the <see cref="IExecutable.Source"/>
    /// </summary>
    /// <returns>A find fluent including the configuration of this executable</returns>
    public virtual IFindFluent<T, T> BuildPipeline()
    {
        var options = Options as FindOptions ?? new FindOptions();
        var filters = FilterDefinition<T>.Empty;

        if (Filters is not null)
        {
            filters = Filters.ToFilterDefinition<T>();
        }

        var pipeline = _collection.Find(filters, options);

        if (Sorting is not null)
        {
            pipeline = pipeline.Sort(Sorting.ToSortDefinition<T>());
        }

        if (Projection is not null)
        {
            pipeline = pipeline.Project<T>(Projection.ToProjectionDefinition<T>());
        }

        return pipeline;
    }
}
