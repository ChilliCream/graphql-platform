namespace HotChocolate.Types;

/// <summary>
/// Provides extension methods to configure the <see cref="Types.Internal"/> directive with the fluent API.
/// </summary>
public static class InternalDirectiveExtensions
{
    /// <summary>
    /// <para>
    /// The @internal directive is used in combination with lookup fields and allows you
    /// to declare internal types and fields. Internal types and fields do not appear in
    /// the final client-facing composite schema and do not participate in the standard
    /// schema-merging process. This allows a source schema to define lookup fields for
    /// resolving entities that should not be accessible through the client-facing
    /// composite schema.
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
    /// </para>
    /// <code>
    /// type User @internal {
    ///   id: ID!
    ///   name: String!
    /// }
    ///
    /// directive @internal on OBJECT | FIELD_DEFINITION
    /// </code>
    /// </summary>
    public static IObjectTypeDescriptor Internal(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Internal.Instance);
    }

    /// <summary>
    /// <para>
    /// The @internal directive is used in combination with lookup fields and allows you
    /// to declare internal types and fields. Internal types and fields do not appear in
    /// the final client-facing composite schema and do not participate in the standard
    /// schema-merging process. This allows a source schema to define lookup fields for
    /// resolving entities that should not be accessible through the client-facing
    /// composite schema.
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--internal"/>
    /// </para>
    /// <code>
    /// type User @internal {
    ///   id: ID!
    ///   name: String!
    /// }
    ///
    /// directive @internal on OBJECT | FIELD_DEFINITION
    /// </code>
    /// </summary>
    public static IObjectFieldDescriptor Internal(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Types.Internal.Instance);
    }
}
