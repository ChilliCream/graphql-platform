using System.Collections.Generic;
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

        public override async ValueTask<IReadOnlyList<T>> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            IBsonSerializerRegistry serializers = _collection.Settings.SerializerRegistry;
            IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

            FindOptions<T> options = Options ?? new FindOptions<T>();

            if (Sorting is not null)
            {
                options.Sort = Sorting.DefaultRender();
            }

            BsonDocument filters = Filters.Render(bsonSerializer, serializers);

            IAsyncCursor<T> cursor = await _collection
                .FindAsync(filters, options, cancellationToken)
                .ConfigureAwait(false);

            return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public override string Print()
        {
            IBsonSerializerRegistry serializers = _collection.Settings.SerializerRegistry;
            IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

            BsonDocument filters = Filters.Render(bsonSerializer, serializers);

            var aggregations = new BsonDocument { { "$match", filters } };

            if (Sorting is not null)
            {
                aggregations["$sort"] = Sorting.DefaultRender();
            }

            return aggregations.ToString();
        }
    }
}
