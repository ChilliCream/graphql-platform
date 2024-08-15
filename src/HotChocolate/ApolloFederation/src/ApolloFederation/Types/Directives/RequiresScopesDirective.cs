using HotChocolate.ApolloFederation.Properties;
using static HotChocolate.ApolloFederation.FederationTypeNames;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @@requiresScopes(scopes: [[Scope!]!]!) on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// Directive that is used to indicate that the target element is accessible only to the authenticated supergraph users with the appropriate JWT scopes.
/// Refer to the <see href = "https://www.apollographql.com/docs/router/configuration/authorization#requiresscopes"> Apollo Router article</see> for additional details.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @requiresScopes(scopes: [["scope1"]])
/// }
/// </example>
/// </summary>
/// <param name="scopes">
/// List of a list of required JWT scopes.
/// </param>
[Package(FederationVersionUrls.Federation25)]
[DirectiveType(
    RequiresScopesDirective_Name,
    DirectiveLocation.Enum |
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.Interface |
    DirectiveLocation.Object |
    DirectiveLocation.Scalar)]
[GraphQLDescription(FederationResources.RequiresScopesDirective_Description)]
public sealed class RequiresScopesDirective(List<IReadOnlySet<Scope>> scopes)
{
    /// <summary>
    /// Retrieves list of a list of required JWT scopes.
    /// </summary>
    [GraphQLType("[[String!]!]!")]
    public List<IReadOnlySet<Scope>> Scopes { get; } = scopes;
}
