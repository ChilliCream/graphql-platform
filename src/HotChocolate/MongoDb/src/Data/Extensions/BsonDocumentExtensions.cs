using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb;

public static class BsonDocumentExtensions
{
    private static readonly IBsonSerializerRegistry _serializerRegistry =
        BsonSerializer.SerializerRegistry;

    private static readonly IBsonSerializer<BsonDocument> _documentSerializer =
        _serializerRegistry.GetSerializer<BsonDocument>();

    public static BsonDocument DefaultRender(
        this FilterDefinition<BsonDocument> bsonQuery)
        => bsonQuery.Render(
            new RenderArgs<BsonDocument>(
                _documentSerializer,
                _serializerRegistry));

    public static BsonDocument DefaultRender(
        this SortDefinition<BsonDocument> bsonQuery)
        => bsonQuery.Render(
            new RenderArgs<BsonDocument>(
                _documentSerializer,
                _serializerRegistry));
}
