using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public static class FilterDefinitionExtensions
{
    public static MongoDbFilterDefinition Wrap<T>(
        this FilterDefinition<T> filterDefinition)
        => new FilterDefinitionWrapper<T>(filterDefinition);

    private sealed class FilterDefinitionWrapper<TDocument>(
        FilterDefinition<TDocument> filter)
        : MongoDbFilterDefinition
    {
        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
            {
                return filter.Render(
                    new RenderArgs<TDocument>(
                        typedSerializer,
                        serializerRegistry));
            }

            return filter.Render(
                new RenderArgs<TDocument>(
                    serializerRegistry.GetSerializer<TDocument>(),
                    serializerRegistry));
        }
    }
}
