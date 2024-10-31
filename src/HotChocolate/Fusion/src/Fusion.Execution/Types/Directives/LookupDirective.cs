using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__lookup(
    schema: fusion__Schema!
    key: fusion__FieldSelectionSet!
    field: fusion__FieldDefinition!
    map: [fusion__FieldSelectionMap!]!
) repeatable on OBJECT | INTERFACE
*/
internal sealed class LookupDirective(
    string schemaName,
    SelectionSetNode key,
    FieldDefinitionNode field,
    ImmutableArray<string> map)
{
    public string SchemaName { get; } = schemaName;

    public SelectionSetNode Key { get; } = key;

    public FieldDefinitionNode Field { get; } = field;

    public ImmutableArray<string> Map { get; } = map;
}
