using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HotChocolate.Data.MongoDb;

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

    public override BsonDocument Render(
        IBsonSerializer<BsonDocument> documentSerializer,
        IBsonSerializerRegistry serializerRegistry,
        LinqProvider provider)
    {
        return Render(documentSerializer, serializerRegistry);
    }

    public ProjectionDefinition<T> ToProjectionDefinition<T>() =>
        new ProjectionDefinitionWrapper<T>(this);

    private sealed class ProjectionDefinitionWrapper<T> : ProjectionDefinition<T>
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

        public override BsonDocument Render(
            IBsonSerializer<T> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider provider)
        {
            return Render(documentSerializer, serializerRegistry);
        }
    }
}
