using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public static class SortDefinitionExtensions
{
    public static MongoDbSortDefinition Wrap<T>(
        this SortDefinition<T> sortDefinition)
        => new SortDefinitionWrapper<T>(sortDefinition);

    private sealed class SortDefinitionWrapper<TDocument>(
        SortDefinition<TDocument> sort)
        : MongoDbSortDefinition
    {
        public override BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
            {
                return sort.Render(
                    new RenderArgs<TDocument>(
                        typedSerializer,
                        serializerRegistry));
            }

            return sort.Render(
                new RenderArgs<TDocument>(
                    serializerRegistry.GetSerializer<TDocument>(),
                    serializerRegistry));
        }
    }
}
