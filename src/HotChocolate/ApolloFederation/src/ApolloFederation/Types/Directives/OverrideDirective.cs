using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// # federation v2.0 definition
/// directive @override(from: String!) on FIELD_DEFINITION
///
/// # federation v2.7 definition
/// directive @override(from: String!, label: String) on FIELD_DEFINITION
/// </code>
///
/// The @override directive is used to indicate that the current subgraph is taking
/// responsibility for resolving the marked field away from the subgraph specified in the from
/// argument. Name of the subgraph to be overridden has to match the name of the subgraph that
/// was used to publish their schema.
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
/// <param name="from">
/// Name of the subgraph to be overridden
/// </param>
[Package(Federation20)]
[DirectiveType(OverrideDirective_Name, DirectiveLocation.FieldDefinition)]
[GraphQLDescription(OverrideDirective_Description)]
[OverrideLegacySupport]
public sealed class OverrideDirective(string from)
{
    /// <summary>
    /// Creates new instance of @override directive.
    /// </summary>
    /// <param name="from">
    /// Name of the subgraph to be overridden
    /// </param>
    /// <param name="label">
    /// Optional label that will be evaluated at runtime to determine whether field should be overridden
    /// </param>
    public OverrideDirective(string from, string? label = null) : this(from)
    {
        Label = label;
    }

    public string From { get; } = from;

    public string? Label { get; }
}
