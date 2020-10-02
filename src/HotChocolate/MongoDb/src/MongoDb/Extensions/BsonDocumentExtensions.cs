using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb
{
    public static class BsonDocumentExtensions
    {
        private static readonly IBsonSerializerRegistry _serializerRegistry =
            BsonSerializer.SerializerRegistry;

        private static readonly IBsonSerializer<BsonDocument> _documentSerializer =
            _serializerRegistry.GetSerializer<BsonDocument>();

        public static BsonDocument DefaultRender(
            this FilterDefinition<BsonDocument> bsonQuery)
        {
            return bsonQuery.Render(_documentSerializer, _serializerRegistry);
        }
    }
}
