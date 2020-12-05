using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    internal static class BsonDocumentExtensions
    {
        private static readonly IBsonSerializerRegistry SerializerRegistry =
            BsonSerializer.SerializerRegistry;

        private static readonly IBsonSerializer<BsonDocument> DocumentSerializer =
            SerializerRegistry.GetSerializer<BsonDocument>();

        public static BsonDocument DefaultRender(
            this FilterDefinition<BsonDocument> bsonQuery)
        {
            return bsonQuery.Render(DocumentSerializer, SerializerRegistry);
        }

        public static BsonDocument DefaultRender(
            this SortDefinition<BsonDocument> bsonQuery)
        {
            return bsonQuery.Render(DocumentSerializer, SerializerRegistry);
        }
    }
}
