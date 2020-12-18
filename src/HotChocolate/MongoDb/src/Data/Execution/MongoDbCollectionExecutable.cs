using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// A executable that is based on <see cref="IMongoCollection{TResult}"/>
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class MongoDbCollectionExecutable<T> : MongoDbExecutable<T>
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDbCollectionExecutable(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public override object Source => _collection;

        /// <summary>
        /// The options that were set by <see cref="WithOptions"/>
        /// </summary>
        protected FindOptionsBase? Options { get; private set; }

        /// <summary>
        /// Applies the options to the executable
        /// </summary>
        /// <param name="options">The options</param>
        /// <returns>A executable that contains the options</returns>
        public IMongoDbExecutable WithOptions(FindOptionsBase options)
        {
            Options = options;
            return this;
        }

        /// <inheritdoc />
        public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            IBsonSerializerRegistry serializers = _collection.Settings.SerializerRegistry;
            IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

            FindOptions<T> options = Options as FindOptions<T> ?? new FindOptions<T>();
            BsonDocument filters = new BsonDocument();

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

            IAsyncCursor<T> cursor = await _collection
                .FindAsync(filters, options, cancellationToken)
                .ConfigureAwait(false);

            return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public override string Print() => BuildPipeline().ToString() ?? "";

        /// <summary>
        /// Applies filtering sorting and projections on the <see cref="IExecutable{T}.Source"/>
        /// </summary>
        /// <returns>A find fluent including the configuration of this executable</returns>
        public IFindFluent<T, T> BuildPipeline()
        {
            FindOptions options = Options as FindOptions ?? new FindOptions();
            FilterDefinition<T> filters = FilterDefinition<T>.Empty;

            if (Filters is not null)
            {
                filters = Filters.ToFilterDefinition<T>();
            }

            IFindFluent<T, T> pipeline = _collection.Find(filters, options);

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
}
