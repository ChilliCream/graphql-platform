using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Preserves which Apollo Federation key fields were originally marked <c>@external</c>
/// before preprocessing promotes them to full field contributions.
/// </summary>
internal sealed class SourceExternalFieldMetadata
{
    private const string MarkerDirectiveName = "fusion__sourceExternal";
    private readonly HashSet<(string TypeName, string FieldName)> _fields;

    private SourceExternalFieldMetadata(
        HashSet<(string TypeName, string FieldName)> fields)
    {
        _fields = fields;
    }

    public static void Capture(MutableSchemaDefinition schema)
    {
        var fields = new HashSet<(string TypeName, string FieldName)>();

        foreach (var coordinate in RemoveExternalFields.CollectKeyReferences(schema))
        {
            if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                    coordinate.TypeName,
                    out var type)
                && type.Fields.TryGetField(coordinate.FieldName, out var field)
                && field.Directives.ContainsName(FederationDirectiveNames.External))
            {
                fields.Add(coordinate);
            }
        }

        schema.Features.Set(new SourceExternalFieldMetadata(fields));
    }

    public static bool Contains(
        MutableSchemaDefinition schema,
        string typeName,
        string fieldName)
        => schema.Features.Get<SourceExternalFieldMetadata>()?._fields.Contains(
            (typeName, fieldName)) == true;

    public static void WriteMarker(MutableSchemaDefinition schema)
    {
        var metadata = schema.Features.Get<SourceExternalFieldMetadata>();
        if (metadata is null || metadata._fields.Count == 0)
        {
            return;
        }

        var markerDefinition = new MutableDirectiveDefinition(MarkerDirectiveName)
        {
            Locations = DirectiveLocation.FieldDefinition
        };
        schema.DirectiveDefinitions.Add(markerDefinition);

        foreach (var (typeName, fieldName) in metadata._fields)
        {
            if (schema.Types.TryGetType<MutableComplexTypeDefinition>(typeName, out var type)
                && type.Fields.TryGetField(fieldName, out var field))
            {
                field.Directives.Add(new Directive(markerDefinition));
            }
        }
    }

    public static void CaptureMarker(MutableSchemaDefinition schema)
    {
        if (!schema.DirectiveDefinitions.ContainsName(MarkerDirectiveName))
        {
            return;
        }

        var fields = new HashSet<(string TypeName, string FieldName)>();

        foreach (var type in schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                var marker = field.Directives.FirstOrDefault(MarkerDirectiveName);
                if (marker is not null)
                {
                    fields.Add((type.Name, field.Name));
                    field.Directives.Remove(marker);
                }
            }
        }

        schema.DirectiveDefinitions.Remove(MarkerDirectiveName);
        schema.Features.Set(new SourceExternalFieldMetadata(fields));
    }
}
