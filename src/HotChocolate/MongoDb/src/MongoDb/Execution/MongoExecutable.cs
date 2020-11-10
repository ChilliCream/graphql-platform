using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public abstract class MongoExecutable<T>
        : IExecutable<T>
        , IMongoExecutable
    {
        protected MongoDbFilterDefinition? Filters { get; private set; }

        protected MongoDbSortDefinition? Sorting { get; private set; }

        protected FindOptionsBase? Options { get; private set; }

        public IMongoExecutable WithOptions(FindOptionsBase options)
        {
            Options = options;
            return this;
        }

        public IMongoExecutable WithFiltering(MongoDbFilterDefinition filters)
        {
            Filters = filters;
            return this;
        }

        public IMongoExecutable WithSorting(MongoDbSortDefinition sorting)
        {
            Sorting = sorting;
            return this;
        }

        public object Source { get; }

        public abstract ValueTask<IList> ToListAsync(CancellationToken cancellationToken);

        public abstract ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);

        public abstract ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken);

        public abstract string Print();

        protected class MongoDbFilterDefinition<TDocument> : FilterDefinition<TDocument>
        {
            private readonly MongoDbFilterDefinition _filter;

            public MongoDbFilterDefinition(MongoDbFilterDefinition filter)
            {
                _filter = filter;
            }

            public override BsonDocument Render(
                IBsonSerializer<TDocument> documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                return _filter.Render(documentSerializer, serializerRegistry);
            }
        }

        protected class MongoDbFilterDefinitionWrapper<TDocument> : MongoDbFilterDefinition
        {
            private readonly FilterDefinition<TDocument> _filter;

            public MongoDbFilterDefinitionWrapper(FilterDefinition<TDocument> filter)
            {
                _filter = filter;
            }

            public override BsonDocument Render(
                IBsonSerializer documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
                {
                    return _filter.Render(typedSerializer, serializerRegistry);
                }

                return _filter.Render(
                    serializerRegistry.GetSerializer<TDocument>(),
                    serializerRegistry);
            }
        }

        protected class MongoDbSortDefinition<TDocument> : SortDefinition<TDocument>
        {
            private readonly MongoDbSortDefinition _sort;

            public MongoDbSortDefinition(MongoDbSortDefinition sort)
            {
                _sort = sort;
            }

            public override BsonDocument Render(
                IBsonSerializer<TDocument> documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                return _sort.Render(documentSerializer, serializerRegistry);
            }
        }

        protected class MongoDbSortDefinitionWrapper<TDocument> : MongoDbSortDefinition
        {
            private readonly SortDefinition<TDocument> _sort;

            public MongoDbSortDefinitionWrapper(SortDefinition<TDocument> sort)
            {
                _sort = sort;
            }

            public override BsonDocument Render(
                IBsonSerializer documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
                {
                    return _sort.Render(typedSerializer, serializerRegistry);
                }

                return _sort.Render(
                    serializerRegistry.GetSerializer<TDocument>(),
                    serializerRegistry);
            }
        }
    }
}
