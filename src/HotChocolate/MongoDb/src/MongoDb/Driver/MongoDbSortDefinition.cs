using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data
{
    public abstract class MongoDbSortDefinition : SortDefinition<BsonDocument>
    {
        public abstract BsonDocument Render(
            IBsonSerializer documentSerializer,
            IBsonSerializerRegistry serializerRegistry);

        public override BsonDocument Render(
            IBsonSerializer<BsonDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return Render(documentSerializer, serializerRegistry);
        }
    }
}
