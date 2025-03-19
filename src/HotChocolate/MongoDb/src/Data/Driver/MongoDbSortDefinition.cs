using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbSortDefinition : MongoDB.Driver.SortDefinition<BsonDocument>
{
    public abstract BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry);

    public override BsonDocument Render(RenderArgs<BsonDocument> args)
        => Render(args.DocumentSerializer, args.SerializerRegistry);

    public MongoDB.Driver.SortDefinition<T> ToSortDefinition<T>() => new SortDefinitionWrapper<T>(this);

    private sealed class SortDefinitionWrapper<TDocument>(
        MongoDbSortDefinition sort)
        : MongoDB.Driver.SortDefinition<TDocument>
    {
        public override BsonDocument Render(RenderArgs<TDocument> args)
            => sort.Render(args.DocumentSerializer, args.SerializerRegistry);
    }
}
