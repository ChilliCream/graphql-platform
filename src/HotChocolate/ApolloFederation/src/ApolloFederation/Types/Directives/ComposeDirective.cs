using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @composeDirective(name: String!) repeatable on SCHEMA
/// </code>
///
/// By default, Supergraph schema excludes all custom directives. The @composeDirective is used to specify
/// custom directives that should be exposed in the Supergraph schema.
///
/// <example>
/// extend schema @composeDirective(name: "@custom")
///   @link(url: "https://specs.apollo.dev/federation/v2.5", import: ["@composeDirective"])
///   @link(url: "https://myspecs.dev/custom/v1.0", import: ["@custom"])
///
/// directive @custom on FIELD_DEFINITION
///
/// type Query {
///   helloWorld: String! @custom
/// }
/// </example>
/// </summary>
[Package(Federation25)]
[DirectiveType(ComposeDirective_Name, DirectiveLocation.Schema, IsRepeatable = true)]
[GraphQLDescription(ComposeDirective_Description)]
public sealed class ComposeDirective(string name)
{    
    public string Name { get; } = name;
}