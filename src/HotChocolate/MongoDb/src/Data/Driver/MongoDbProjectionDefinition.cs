using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbProjectionDefinition : ProjectionDefinition<BsonDocument>
{
    public abstract BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry);

    public override BsonDocument Render(RenderArgs<BsonDocument> args)
        => Render(args.DocumentSerializer, args.SerializerRegistry);

    public ProjectionDefinition<T> ToProjectionDefinition<T>() =>
        new ProjectionDefinitionWrapper<T>(this);

    private sealed class ProjectionDefinitionWrapper<T>(
        MongoDbProjectionDefinition filter)
        : ProjectionDefinition<T>
    {
        public override BsonDocument Render(RenderArgs<T> args)
            => filter.Render(args.DocumentSerializer, args.SerializerRegistry);
    }
}
