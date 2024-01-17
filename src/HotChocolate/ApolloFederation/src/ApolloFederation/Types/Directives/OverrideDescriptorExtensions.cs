namespace HotChocolate.ApolloFederation.Types;

public static class OverrideDescriptorExtensions
{
    /// <summary>
    /// Applies the @override directive which is used to indicate that the current subgraph is taking
    /// responsibility for resolving the marked field away from the subgraph specified in the from
    /// argument. Name of the subgraph to be overridden has to match the name of the subgraph that
    /// was used to publish their schema.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   description: String @override(from: "BarSubgraph")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="from">
    /// Name of the subgraph to be overridden.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="from"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IObjectFieldDescriptor Override(
        this IObjectFieldDescriptor descriptor,
        string from)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(from);

        return descriptor.Directive(new OverrideDirective(from));
    }
}