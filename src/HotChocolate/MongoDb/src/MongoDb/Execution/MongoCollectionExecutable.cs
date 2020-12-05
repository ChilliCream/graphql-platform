using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public class MongoCollectionExecutable<T> : MongoExecutable<T>
    {
        private readonly IMongoCollection<T> _collection;

        public MongoCollectionExecutable(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

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

            if (Projections is not null)
            {
                options.Projection = Projections.Render(bsonSerializer, serializers);
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

        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        public override string Print() => BuildPipeline().ToString() ?? "";

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

            if (Projections is not null)
            {
                pipeline = pipeline.Project<T>(Projections.ToProjectionDefinition<T>());
            }

            return pipeline;
        }
    }
}
