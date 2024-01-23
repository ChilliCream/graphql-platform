namespace HotChocolate.ApolloFederation.Types;

public static class ExternalDescriptorExtensions
{
    /// <summary>
    /// Applies the @external directive which is used to mark a field as owned by another service.
    /// This allows service A to use fields from service B while also knowing at runtime
    /// the types of that field. All the external fields should either be referenced from the @key,
    /// @requires or @provides directives field sets.
    ///
    /// Due to the smart merging of entity types, Federation v2 no longer requires @external directive
    /// on @key fields and can be safely omitted from the schema. @external directive is only required
    /// on fields referenced by the @requires and @provides directive.
    ///
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   remoteField: String @external
    ///   localField: String @requires(fields: "remoteField")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor External(
        this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(ExternalDirective.Default);
    }
}