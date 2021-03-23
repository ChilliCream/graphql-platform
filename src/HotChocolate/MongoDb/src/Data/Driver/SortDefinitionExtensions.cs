using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    internal static class SortDefinitionExtensions
    {
        public static MongoDbSortDefinition Wrap<T>(this SortDefinition<T> sortDefinition) =>
            new SortDefinitionWrapper<T>(sortDefinition);

        private class SortDefinitionWrapper<TDocument> : MongoDbSortDefinition
        {
            private readonly SortDefinition<TDocument> _sort;

            public SortDefinitionWrapper(SortDefinition<TDocument> sort)
            {
                _sort = sort;
            }

            public override BsonDocument Render(
                IBsonSerializer documentSerializer,
                IBsonSerializerRegistry serializerRegistry)
            {
                if (documentSerializer is IBsonSerializer<TDocument> typedSerializer)
                {
                    return _sort.Render(typedSerializer, serializerRegistry);
                }

                return _sort.Render(
                    serializerRegistry.GetSerializer<TDocument>(),
                    serializerRegistry);
            }
        }
    }
}
