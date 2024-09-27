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
    {
        return Render(args.DocumentSerializer, args.SerializerRegistry);
    }

    public SortDefinition<T> ToSortDefinition<T>() => new SortDefinitionWrapper<T>(this);

    private sealed class SortDefinitionWrapper<TDocument> : SortDefinition<TDocument>
    {
        private readonly MongoDbSortDefinition _sort;

        public SortDefinitionWrapper(MongoDbSortDefinition sort)
        {
            _sort = sort;
        }

        public override BsonDocument Render(RenderArgs<TDocument> args)
        {
            return _sort.Render(args.DocumentSerializer, args.SerializerRegistry);
        }
    }
}
