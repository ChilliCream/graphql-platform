using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbFilterDefinition : FilterDefinition<BsonDocument>
{
    private static readonly MongoDbFilterDefinition _empty =  new MongoDbEmptyFilterDefinition();

    /// <summary>
    /// Gets an empty filter. An empty filter matches everything.
    /// </summary>
    public new static MongoDbFilterDefinition Empty => _empty;

    public abstract BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry);

    public override BsonDocument Render(RenderArgs<BsonDocument> args)
        => Render(args.DocumentSerializer, args.SerializerRegistry);

    public FilterDefinition<T> ToFilterDefinition<T>() => new FilterDefinitionWrapper<T>(this);

    private sealed class FilterDefinitionWrapper<T>(
        MongoDbFilterDefinition filter)
        : FilterDefinition<T>
    {
        public override BsonDocument Render(RenderArgs<T> args)
            => filter.Render(args.DocumentSerializer, args.SerializerRegistry);
    }

    private sealed class MongoDbEmptyFilterDefinition : MongoDbFilterDefinition
    {
        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
            => new();
    }
}
