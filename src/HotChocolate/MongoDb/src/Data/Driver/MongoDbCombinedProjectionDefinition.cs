using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace HotChocolate.Data.MongoDb;

/// <summary>
/// This class was ported over from the official mongo db driver
/// </summary>
public sealed class MongoDbCombinedProjectionDefinition : MongoDbProjectionDefinition
{
    private readonly MongoDbProjectionDefinition[] _projections;

    public MongoDbCombinedProjectionDefinition(
        params MongoDbProjectionDefinition[] projections)
    {
        _projections = projections;
    }

    public MongoDbCombinedProjectionDefinition(
        IEnumerable<MongoDbProjectionDefinition> projections)
    {
        _projections = Ensure.IsNotNull(projections, nameof(projections)).ToArray();
    }

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

        foreach (var projection in _projections)
        {
            if (!projection.TryRender(documentSerializer, serializerRegistry, out var renderedProjection))
            {
                document.Clear();
                return false;
            }

            foreach (var element in renderedProjection.Elements)
            {
                document.Remove(element.Name);
                document.Add(element);
            }
        }

        RemoveCoveredInclusions(document);

        return true;
    }

    private static void RemoveCoveredInclusions(BsonDocument document)
    {
        for (var i = document.ElementCount - 1; i >= 0; i--)
        {
            var element = document.GetElement(i);

            if (element.Name.Contains('.') && IsInclusion(element.Value) && IsCovered(document, element.Name))
            {
                document.RemoveAt(i);
            }
        }
    }

    private static bool IsCovered(BsonDocument document, string path)
    {
        foreach (var element in document.Elements)
        {
            var parentPath = element.Name;

            if (path.Length > parentPath.Length
                && path[parentPath.Length] == '.'
                && path.StartsWith(parentPath, StringComparison.Ordinal)
                && IsInclusion(element.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInclusion(BsonValue value)
        => value.IsBoolean ? value.AsBoolean : value.IsNumeric && value.ToDouble() != 0;
}
