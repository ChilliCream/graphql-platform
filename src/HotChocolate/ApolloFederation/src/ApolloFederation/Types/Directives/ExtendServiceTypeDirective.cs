using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @extends on OBJECT | INTERFACE
/// </code>
///
/// The @extends directive is used to represent type extensions in the schema. Federated extended types should have
/// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
/// <example>
/// # extended from the Users service
/// type Foo @extends @key(fields: "id") {
///   id: ID
///   description: String
/// }
/// </example>
/// </summary>
[Package(Federation20)]
[DirectiveType(
    ExtendsDirective_Name,
    DirectiveLocation.Object |
    DirectiveLocation.Interface)]
[GraphQLDescription(ExtendsDirective_Description)]
public sealed class ExtendServiceTypeDirective
{
    public static ExtendServiceTypeDirective Default { get; } = new();
}
