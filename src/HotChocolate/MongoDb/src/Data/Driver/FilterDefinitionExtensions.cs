using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    internal static class FilterDefinitionExtensions
    {
        public static MongoDbFilterDefinition Wrap<T>(this FilterDefinition<T> sortDefinition) =>
            new FilterDefinitionWrapper<T>(sortDefinition);

        private class FilterDefinitionWrapper<TDocument> : MongoDbFilterDefinition
        {
            private readonly FilterDefinition<TDocument> _sort;

            public FilterDefinitionWrapper(FilterDefinition<TDocument> sort)
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
