using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Preserves which Apollo Federation key fields were originally marked <c>@external</c>
/// before preprocessing promotes them to full field contributions. Only fields declared
/// on plain types are recorded: there the subgraph merely echoes caller-supplied key
/// values, so the field is entry/echo-only and marked <c>sourceExternal</c> for the
/// planner. On an extension type (the <c>@extends</c> directive or <c>extend type</c>
/// syntax, the Fed-1 entity extension idiom) a key-referenced external field is a full
/// contribution backed by the subgraph's own storage and carries no marker.
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
                && !IsExtensionType(type)
                && type.Fields.TryGetField(coordinate.FieldName, out var field)
                && field.Directives.ContainsName(FederationDirectiveNames.External))
            {
                fields.Add(coordinate);
            }
        }

        schema.Features.Set(new SourceExternalFieldMetadata(fields));
    }

    /// <summary>
    /// Determines whether the type is an entity extension. Capture runs before
    /// <see cref="RemoveFederationInfrastructure"/> strips <c>@extends</c>, so the directive
    /// spelling is still visible; the <c>extend type</c> SDL spelling survives as the
    /// <see cref="HotChocolate.Features.TypeExtensionMarker"/> feature the schema parser sets
    /// when no base type definition exists in the source text.
    /// </summary>
    private static bool IsExtensionType(MutableComplexTypeDefinition type)
        => type.Directives.ContainsName(FederationDirectiveNames.Extends)
            || type.IsTypeExtension();

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
