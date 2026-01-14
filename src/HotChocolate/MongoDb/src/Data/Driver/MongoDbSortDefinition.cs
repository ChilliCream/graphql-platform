using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbSortDefinition : SortDefinition<BsonDocument>
{
    public abstract BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry);

    public override BsonDocument Render(RenderArgs<BsonDocument> args)
        => Render(args.DocumentSerializer, args.SerializerRegistry);

    public SortDefinition<T> ToSortDefinition<T>() => new SortDefinitionWrapper<T>(this);

    private sealed class SortDefinitionWrapper<TDocument>(
        MongoDbSortDefinition sort)
        : SortDefinition<TDocument>
    {
        public override BsonDocument Render(RenderArgs<TDocument> args)
            => sort.Render(args.DocumentSerializer, args.SerializerRegistry);
    }
}
