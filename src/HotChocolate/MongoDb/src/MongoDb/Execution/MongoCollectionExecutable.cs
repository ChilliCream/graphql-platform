using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
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
            BsonDocument filters = new BsonDocument();

            if (Sorting is not null)
            {
                options.Sort = Sorting.Render(bsonSerializer, serializers);
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

        public override string Print()
        {
            IBsonSerializerRegistry serializers = _collection.Settings.SerializerRegistry;
            IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

            BsonDocument filters = new BsonDocument();

            if (Filters is not null)
            {
                filters = Filters.Render(bsonSerializer, serializers);
            }

            var aggregations = new BsonDocument { { "$match", filters } };

            if (Sorting is not null)
            {
                aggregations["$sort"] = Sorting.Render(bsonSerializer, serializers);
            }

            return aggregations.ToString();
        }
    }

    public class MongoFluentExecutable<T> : MongoExecutable<T>
    {
        private readonly IAggregateFluent<T> _aggregate;

        public MongoFluentExecutable(IAggregateFluent<T> aggregate)
        {
            _aggregate = aggregate;
        }

        public override async ValueTask<IReadOnlyList<T>> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            IAggregateFluent<T> pipeline = _aggregate;
            if (Sorting is not null)
            {
                pipeline = pipeline.Sort(new MongoDbAggregateFluentSortDefinition<T>(Sorting));
            }

            if (Filters is not null)
            {
                pipeline = pipeline.Match(new MongoDbAggregateFluentFilterDefinition<T>(Filters));
            }

            return await pipeline.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public override string Print()
        {
            IAggregateFluent<T> pipeline = _aggregate;

            if (Filters is not null)
            {
                pipeline = pipeline.Match(new MongoDbAggregateFluentFilterDefinition<T>(Filters));
            }

            if (Sorting is not null)
            {
                pipeline = pipeline.Sort(new MongoDbAggregateFluentSortDefinition<T>(Sorting));
            }

            return pipeline.ToString() ?? "";
        }

        private class MongoDbAggregateFluentFilterDefinition<T> : FilterDefinition<T>
        {
            private readonly MongoDbFilterDefinition _filter;

            public MongoDbAggregateFluentFilterDefinition(MongoDbFilterDefinition filter)
            {
                _filter = filter;
            }

            public override BsonDocument Render(
                IBsonSerializer<T> documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                return _filter.Render(documentSerializer, serializerRegistry);
            }
        }

        private class MongoDbAggregateFluentSortDefinition<T> : SortDefinition<T>
        {
            private readonly MongoDbSortDefinition _filter;

            public MongoDbAggregateFluentSortDefinition(MongoDbSortDefinition filter)
            {
                _filter = filter;
            }

            public override BsonDocument Render(
                IBsonSerializer<T> documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                return _filter.Render(documentSerializer, serializerRegistry);
            }
        }
    }
}
