using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__field(
    schema: fusion__Schema!
    sourceName: String
    sourceType: Type
    provides: FieldSelectionSet
    external: Boolean! = false
) repeatable on FIELD_DEFINITION
*/
internal class FieldDirective(
    SchemaKey schemaKey,
    string? sourceName,
    ITypeNode? sourceType,
    SelectionSetNode? provides,
    bool isExternal)
{
    public SchemaKey SchemaKey { get; } = schemaKey;

    public string? SourceName { get; } = sourceName;

    public ITypeNode? SourceType { get; } = sourceType;

    public SelectionSetNode? Provides { get; } = provides;

    public bool IsExternal { get; } = isExternal;
}
