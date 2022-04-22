using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public static class FilterDefinitionExtensions
{
    public static MongoDbFilterDefinition Wrap<T>(
        this FilterDefinition<T> filterDefinition) =>
        new FilterDefinitionWrapper<T>(filterDefinition);

    private sealed class FilterDefinitionWrapper<TDocument> : MongoDbFilterDefinition
    {
        private readonly FilterDefinition<TDocument> _filter;

        public FilterDefinitionWrapper(FilterDefinition<TDocument> filter)
        {
            _filter = filter;
        }

        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
            {
                return _filter.Render(typedSerializer, serializerRegistry);
            }

            return _filter.Render(
                serializerRegistry.GetSerializer<TDocument>(),
                serializerRegistry);
        }
    }
}
