using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @authenticated on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// The @authenticated directive is used to indicate that the target element is accessible only to the authenticated supergraph users.
/// For more granular access control, see the <see cref="RequiresScopesDirective">RequiresScopeDirectiveType</see> directive usage.
/// Refer to the <see href="https://www.apollographql.com/docs/router/configuration/authorization#authenticated">Apollo Router article</see>
/// for additional details.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @authenticated
/// }
/// </example>
/// </summary>
[Package(Federation24)]
[DirectiveType(
    AuthenticatedDirective_Name, 
    DirectiveLocation.Enum |
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.Interface |
    DirectiveLocation.Object |
    DirectiveLocation.Scalar)]
[GraphQLDescription(AuthenticatedDirective_Description)]
public sealed class AuthenticatedDirective
{
    public static AuthenticatedDirective Default { get; } = new();
}
