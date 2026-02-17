using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__lookup(
     schema: fusion__Schema!
     key: fusion__FieldSelectionSet!
     field: fusion__FieldDefinition!
     map: [fusion__FieldSelectionMap!]!
     path: fusion__FieldSelectionPath
     internal: Boolean! = false
   ) repeatable on OBJECT | INTERFACE | UNION
*/
internal sealed class LookupDirective(
    SchemaKey schemaKey,
    SelectionSetNode key,
    FieldDefinitionNode field,
    ImmutableArray<string> map,
    ImmutableArray<string> path,
    bool @internal = false)
{
    public SchemaKey SchemaKey { get; } = schemaKey;

    public SelectionSetNode Key { get; } = key;

    public FieldDefinitionNode Field { get; } = field;

    public ImmutableArray<string> Map { get; } = map;

    public ImmutableArray<string> Path { get; } = path;

    public bool Internal { get; } = @internal;
}
