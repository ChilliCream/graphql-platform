using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HotChocolate.Data.MongoDb;

public abstract class MongoDbFilterDefinition : FilterDefinition<BsonDocument>
{
    private static readonly MongoDbFilterDefinition _empty =  new MongoDbEmptyFilterDefinition();

    /// <summary>
    /// Gets an empty filter. An empty filter matches everything.
    /// </summary>
    public static new MongoDbFilterDefinition Empty => MongoDbFilterDefinition._empty;

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

    public FilterDefinition<T> ToFilterDefinition<T>() => new FilterDefinitionWrapper<T>(this);

    private sealed class FilterDefinitionWrapper<T> : FilterDefinition<T>
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

        public override BsonDocument Render(
            IBsonSerializer<T> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            LinqProvider provider)
        {
            return Render(documentSerializer, serializerRegistry);
        }
    }

    internal sealed class MongoDbEmptyFilterDefinition : MongoDbFilterDefinition
    {
        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return new BsonDocument();
        }
    }
}
