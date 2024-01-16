using HotChocolate.ApolloFederation.Types;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// directive @override(from: String!) on FIELD_DEFINITION
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
/// </summary>
[DirectiveType(DirectiveLocation.FieldDefinition)]
[GraphQLDescription(Descriptions.OverrideDirective_Description)]
public sealed class OverrideDirective(string from)
{
    public string From { get; } = from;
}