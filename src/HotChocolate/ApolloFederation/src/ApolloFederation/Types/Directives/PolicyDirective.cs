using HotChocolate.ApolloFederation.Properties;
using static HotChocolate.ApolloFederation.FederationTypeNames;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @policy(policies: [[Policy!]!]!) on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// Indicates to composition that the target element is restricted based on authorization policies that are evaluated in a Rhai script or coprocessor.
/// Refer to the <see href = "https://www.apollographql.com/docs/router/configuration/authorization#policy"> Apollo Router article</see> for additional details.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @policy(policies: [["policy1"]])
/// }
/// </example>
/// </summary>
/// <param name="policies">
/// List of a list of authorization policies to evaluate.
/// </param>
[Package(FederationVersionUrls.Federation26)]
[DirectiveType(
    PolicyDirective_Name,
    DirectiveLocation.Enum |
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.Interface |
    DirectiveLocation.Object |
    DirectiveLocation.Scalar)]
[GraphQLDescription(FederationResources.PolicyDirective_Description)]
public sealed class PolicyDirective(List<IReadOnlySet<Policy>> policies)
{
    /// <summary>
    /// Retrieves list of a list of authorization policies to evaluate.
    /// </summary>
    [GraphQLType("[[String!]!]!")]
    public List<IReadOnlySet<Policy>> Policies { get; } = policies;
}
