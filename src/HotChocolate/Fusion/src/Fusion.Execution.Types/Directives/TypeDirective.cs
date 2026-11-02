namespace HotChocolate.Fusion.Types.Directives;

/*
directive @fusion__type(
    schema: fusion__Schema!
) repeatable on OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT | SCALAR
*/
internal sealed class TypeDirective(SchemaKey schemaKey)
{
    public SchemaKey SchemaKey { get; } = schemaKey;
}
