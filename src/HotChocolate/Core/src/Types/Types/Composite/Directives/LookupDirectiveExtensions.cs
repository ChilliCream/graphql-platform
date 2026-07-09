namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to configure the <see cref="Lookup"/> directive with the fluent API.
/// </summary>
public static class LookupDirectiveExtensions
{
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
    public static IObjectFieldDescriptor Lookup(this IObjectFieldDescriptor descriptor)
        => descriptor.Directive(Composite.Lookup.Instance);
}
