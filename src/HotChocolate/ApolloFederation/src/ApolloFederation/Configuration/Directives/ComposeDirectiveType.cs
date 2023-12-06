using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;

namespace HotChocolate.ApolloFederation;

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
public sealed class ComposeDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownTypeNames.ComposeDirective)
            .Description(FederationResources.ComposeDirective_Description)
            .Location(DirectiveLocation.Schema)
            .Argument(WellKnownArgumentNames.Name)
            .Type<NonNullType<StringType>>();
}
