using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data
{
    public abstract class MongoDbFilterDefinition : FilterDefinition<BsonDocument>
    {
        public abstract BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry);

        public override BsonDocument Render(
            IBsonSerializer<BsonDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return Render(documentSerializer, serializerRegistry);
        }

        public FilterDefinition<T> ToFilterDefinition<T>() => new FilterDefinitionWrapper<T>(this);

        private class FilterDefinitionWrapper<T> : FilterDefinition<T>
        {
            private readonly MongoDbFilterDefinition _filter;

            public FilterDefinitionWrapper(MongoDbFilterDefinition filter)
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
