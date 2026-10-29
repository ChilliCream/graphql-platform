using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbProjectionDefinition : ProjectionDefinition<BsonDocument>
{
    public abstract BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry);

    /// <summary>
    /// Tries to render a projection. Returns false when the whole document is required.
    /// </summary>
    internal virtual bool TryRender(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry,
        out BsonDocument document)
    {
        document = Render(documentSerializer, serializerRegistry);
        return true;
    }

    public override BsonDocument Render(RenderArgs<BsonDocument> args)
        => Render(args.DocumentSerializer, args.SerializerRegistry);

    public ProjectionDefinition<T> ToProjectionDefinition<T>() =>
        new ProjectionDefinitionWrapper<T>(this);

    private sealed class ProjectionDefinitionWrapper<T>(
        MongoDbProjectionDefinition filter)
        : ProjectionDefinition<T>
    {
        public override BsonDocument? Render(RenderArgs<T> args)
            => filter.TryRender(args.DocumentSerializer, args.SerializerRegistry, out var document)
                ? document
                : null;
    }
}
