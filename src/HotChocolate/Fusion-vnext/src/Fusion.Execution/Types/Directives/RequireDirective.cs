using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__requires(
    schema: fusion__Schema!
    field: fusion__FieldDefinition!
    map: [fusion__FieldSelectionMap!]!
) repeatable on FIELD_DEFINITION
*/
internal class RequireDirective(
    string schemaName,
    FieldDefinitionNode field,
    ImmutableArray<string> map)
{
    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public FieldDefinitionNode Field { get; } = field;

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<string> Map { get; } = map;
}
