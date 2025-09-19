namespace HotChocolate.ApolloFederation.Types;

public static class RequiresDescriptorExtensions
{
    /// <summary>
    /// Applies the @requires directive which is used to specify external (provided by other subgraphs)
    /// entity fields that are needed to resolve target field. It is used to develop a query plan where
    /// the required fields may not be needed by the client, but the service may need additional
    /// information from other subgraphs. Required fields specified in the directive field set should
    /// correspond to a valid field on the underlying GraphQL interface/object and should be instrumented
    /// with @external directive.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   # this field will be resolved from other subgraph
    ///   remote: String @external
    ///   local: String @requires(fields: "remote")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The <paramref name="fieldSet"/> describes which fields may
    /// not be needed by the client, but are required by
    /// this service as additional information from other services.
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
    public static IObjectFieldDescriptor Requires(
        this IObjectFieldDescriptor descriptor,
        string fieldSet)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);

        return descriptor.Directive(new RequiresDirective(fieldSet));
    }
}
