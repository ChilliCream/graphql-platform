namespace HotChocolate.Types;

/// <summary>
/// <para>
/// The @lookup directive is used within a source schema to specify output fields
/// that can be used by the distributed GraphQL executor to resolve an entity by
/// a stable key.
/// </para>
/// <para>
/// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--lookup"/>
/// </para>
/// <code>
/// type Query {
///   productById(id: ID!): Product @lookup
/// }
///
/// directive @lookup on FIELD_DEFINITION
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.Lookup.Name,
    DirectiveLocation.FieldDefinition,
    IsRepeatable = false)]
public sealed class Lookup
{
    private Lookup()
    {
    }

    /// <summary>
    /// The singleton instance of the <see cref="Lookup"/> directive.
    /// </summary>
    public static Lookup Instance { get; } = new();
}
