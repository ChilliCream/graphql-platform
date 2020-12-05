using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data
{
    public abstract class MongoDbProjectionDefinition : ProjectionDefinition<BsonDocument>
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

        public ProjectionDefinition<T> ToProjectionDefinition<T>() =>
            new ProjectionDefinitionWrapper<T>(this);

        private class ProjectionDefinitionWrapper<T> : ProjectionDefinition<T>
        {
            private readonly MongoDbProjectionDefinition _filter;

            public ProjectionDefinitionWrapper(MongoDbProjectionDefinition filter)
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
