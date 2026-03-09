using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public static class BsonDocumentExtensions
{
    private static readonly IBsonSerializerRegistry s_serializerRegistry =
        BsonSerializer.SerializerRegistry;

    private static readonly IBsonSerializer<BsonDocument> s_documentSerializer =
        s_serializerRegistry.GetSerializer<BsonDocument>();

    public static BsonDocument DefaultRender(
        this FilterDefinition<BsonDocument> bsonQuery)
        => bsonQuery.Render(
            new RenderArgs<BsonDocument>(
                s_documentSerializer,
                s_serializerRegistry));

    public static BsonDocument DefaultRender(
        this SortDefinition<BsonDocument> bsonQuery)
        => bsonQuery.Render(
            new RenderArgs<BsonDocument>(
                s_documentSerializer,
                s_serializerRegistry));
}
