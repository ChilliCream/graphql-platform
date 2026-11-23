using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace HotChocolate.Data.MongoDb;

internal sealed class MongoDbRequiredProjectionOperation(string path) : MongoDbProjectionDefinition
{
    public override BsonDocument Render(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry)
    {
        TryRender(documentSerializer, serializerRegistry, out var document);
        return document;
    }

    internal override bool TryRender(
        IBsonSerializer documentSerializer,
        IBsonSerializerRegistry serializerRegistry,
        out BsonDocument document)
    {
        document = new BsonDocument();
        var serializer = documentSerializer;
        string? resolvedPath = null;

        for (var start = 0; start < path.Length;)
        {
            while (serializer is IBsonArraySerializer arraySerializer
                && arraySerializer.TryGetItemSerializationInfo(out var itemInfo))
            {
                serializer = itemInfo.Serializer;
            }

            var end = path.IndexOf('.', start);

            if (end < 0)
            {
                end = path.Length;
            }

            if (serializer is IDiscriminatedInterfaceSerializer or BsonDocumentSerializer
                || serializer is not IBsonDocumentSerializer memberSerializer
                || !memberSerializer.TryGetMemberSerializationInfo(path[start..end], out var memberInfo))
            {
                break;
            }

            IncludeDiscriminator(document, serializer, resolvedPath);
            resolvedPath = resolvedPath is null
                ? memberInfo.ElementName
                : resolvedPath + "." + memberInfo.ElementName;
            serializer = memberInfo.Serializer;
            start = end + 1;
        }

        if (resolvedPath is null)
        {
            document.Clear();
            return false;
        }

        document.Remove(resolvedPath);
        document.InsertAt(0, new BsonElement(resolvedPath, 1));
        return true;
    }

    private static void IncludeDiscriminator(
        BsonDocument document,
        IBsonSerializer serializer,
        string? parentPath)
    {
        if (serializer is IHasDiscriminatorConvention)
        {
            var discriminatorName = serializer.GetDiscriminatorConvention().ElementName;
            var discriminatorPath = parentPath is null
                ? discriminatorName
                : parentPath + "." + discriminatorName;

            document[discriminatorPath] = 1;
        }
    }
}
