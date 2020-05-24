using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class MongoFilterVisitorContextExtensions
    {
        private static IBsonSerializerRegistry _serializerRegistry =
            BsonSerializer.SerializerRegistry;
        private static IBsonSerializer<BsonDocument> _documentSerializer =
            _serializerRegistry.GetSerializer<BsonDocument>();

        public static MongoFilterScope GetMongoFilterScope(
                this MongoFilterVisitorContext context) =>
                    (MongoFilterScope)context.GetScope();

        public static bool TryCreateQuery(
           this MongoFilterVisitorContext context,
           [NotNullWhen(true)] out BsonDocument? query)
        {
            if (context.GetMongoFilterScope().TryCreateQuery(
                out FilterDefinition<BsonDocument>? bsonQuery))
            {
                query = bsonQuery.Render(_documentSerializer, _serializerRegistry);
                return true;
            }
            query = null;
            return false;
        }
    }
}
