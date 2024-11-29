namespace HotChocolate.ApolloFederation.Types;

public static class ProvidesDescriptorExtensions
{
    /// <summary>
    /// Applies the @provides directive which is a router optimization hint specifying field set that
    /// can be resolved locally at the given subgraph through this particular query path. This
    /// allows you to expose only a subset of fields from the underlying entity type to be selectable
    /// from the federated schema without the need to call other subgraphs. Provided fields specified
    /// in the directive field set should correspond to a valid field on the underlying GraphQL
    /// interface/object type. @provides directive can only be used on fields returning entities.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///     id: ID!
    ///     # implies name field can be resolved locally
    ///     bar: Bar @provides(fields: "name")
    ///     # name fields are external
    ///     # so will be fetched from other subgraphs
    ///     bars: [Bar]
    /// }
    ///
    /// type Bar @key(fields: "id") {
    ///     id: ID!
    ///     name: String @external
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The fields that are guaranteed to be selectable by the gateway.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Provides(
        this IObjectFieldDescriptor descriptor,
        string fieldSet)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);
        return descriptor.Directive(new ProvidesDirective(fieldSet));
    }
}
