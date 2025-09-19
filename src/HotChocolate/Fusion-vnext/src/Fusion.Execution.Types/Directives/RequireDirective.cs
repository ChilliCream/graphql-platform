using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__requires(
    schema: fusion__Schema!
    requirements: fusion__FieldSelectionSet!
    field: fusion__FieldDefinition!
    map: [fusion__FieldSelectionMap]!
) repeatable on FIELD_DEFINITION
*/
internal class RequireDirective(
    SchemaKey schemaKey,
    SelectionSetNode requirements,
    FieldDefinitionNode field,
    ImmutableArray<string?> map)
{
    /// <summary>
    /// Gets the name of the source schema that has requirements. for a field.
    /// </summary>
    public SchemaKey SchemaKey { get; } = schemaKey;

    /// <summary>
    /// Gets the requirements for a field.
    /// </summary>
    public SelectionSetNode Requirements { get; } = requirements;

    /// <summary>
    /// Gets the arguments that represent field requirements.
    /// </summary>
    public FieldDefinitionNode Field { get; } = field;

    /// <summary>
    /// Gets the paths to the field that are required.
    /// </summary>
    public ImmutableArray<string?> Map { get; } = map;
}
