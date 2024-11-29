using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @override(from: String!) on FIELD_DEFINITION
/// </code>
///
/// The @override directive is used to indicate that the current subgraph is taking
/// responsibility for resolving the marked field away from the subgraph specified in the from
/// argument. Name of the subgraph to be overridden has to match the name of the subgraph that
/// was used to publish their schema.
///
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   description: String @override(from: "BarSubgraph")
/// }
/// </example>
///
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
/// <remarks>
/// Initializes new instance of <see cref="OverrideAttribute"/>
/// </remarks>
/// <param name="from">
/// Name of the subgraph to be overridden
/// </param>
/// <param name="label">
/// Optional label that will be evaluated at runtime to determine whether field should be overridden
/// </param>
public sealed class OverrideAttribute(string from, string? label = null) : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// Get name of the subgraph to be overridden.
    /// </summary>
    public string From { get; } = from;

    /// <summary>
    /// Get optional label that will be evaluated at runtime to determine whether field should be overridden.
    /// </summary>
    public string? Label { get; } = label;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Override(From, Label);
    }
}
