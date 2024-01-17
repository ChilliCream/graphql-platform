using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.FederationVersionUrls;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @interfaceObject on OBJECT
/// </code>
///
/// The @interfaceObject directive provides meta information to the router that this entity
/// type defined within this subgraph is an interface in the supergraph. This allows you to extend functionality
/// of an interface across the supergraph without having to implement (or even be aware of) all its implementing types.
/// <example>
/// type Foo @interfaceObject @key(fields: "ids") {
///   id: ID!
///   newCommonField: String
/// }
/// </example>
/// </summary>
[Package(Federation22)]
[DirectiveType(InterfaceObject_Name, DirectiveLocation.Object)]
[GraphQLDescription(InterfaceObjectDirective_Description)]
public sealed class InterfaceObjectDirective
{
    public static InterfaceObjectDirective Default { get; } = new();
}