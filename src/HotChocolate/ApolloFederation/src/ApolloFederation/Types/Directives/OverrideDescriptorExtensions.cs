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
    /// The progressive @override feature enables the gradual, progressive deployment of a subgraph
    /// with an @override field. As a subgraph developer, you can customize the percentage of traffic
    /// that the overriding and overridden subgraphs each resolve for a field. You apply a label to
    /// an @override field to set the percentage of traffic for the field that should be resolved by
    /// the overriding subgraph, with the remaining percentage resolved by the overridden subgraph.
    /// See <see href = "https://www.apollographql.com/docs/federation/entities-advanced/#incremental-migration-with-progressive-override">Apollo documentation</see>
    /// for additional details.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   description: String @override(from: "BarSubgraph", label: "percent(1)")
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="from">
    /// Name of the subgraph to be overridden.
    /// </param>
    /// <param name="label">
    /// Optional label that will be used at runtime to evaluate whether to override the field or not.
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
        string from,
        string? label = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(from);

        return descriptor.Directive(new OverrideDirective(from, label));
    }
}
