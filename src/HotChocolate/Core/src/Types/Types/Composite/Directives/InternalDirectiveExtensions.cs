namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides extension methods to configure the <see cref="Composite.Internal"/> directive with the fluent API.
/// </summary>
public static class InternalDirectiveExtensions
{
    /// <summary>
    /// <para>
    /// Applies the @internal directive to the object type to declare it as an internal type.
    /// Internal types and fields do not appear in the final client-facing composite schema and
    /// do not participate in the standard schema-merging process. This allows a source schema to
    /// define lookup fields for resolving entities that should not be accessible through the
    /// client-facing composite schema.
    /// </para>
    /// <para>
    /// <code language="graphql">
    /// type User @internal {
    ///   id: ID!
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
    /// </para>
    /// </summary>
    public static IObjectTypeDescriptor Internal(this IObjectTypeDescriptor descriptor)
        => descriptor.Directive(Composite.Internal.Instance);

    /// <summary>
    /// <para>
    /// Applies the @internal directive to the object field to declare it as an internal field.
    /// Internal types and fields do not appear in the final client-facing composite schema and
    /// do not participate in the standard schema-merging process. This allows a source schema to
    /// define lookup fields for resolving entities that should not be accessible through the
    /// client-facing composite schema.
    /// </para>
    /// <para>
    /// <para>
    /// <code language="graphql">
    /// type User {
    ///   id: ID! @internal
    ///   name: String!
    /// }
    /// </code>
    /// </para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
    /// </para>
    /// </summary>
    public static IObjectFieldDescriptor Internal(this IObjectFieldDescriptor descriptor)
        => descriptor.Directive(Composite.Internal.Instance);
}
