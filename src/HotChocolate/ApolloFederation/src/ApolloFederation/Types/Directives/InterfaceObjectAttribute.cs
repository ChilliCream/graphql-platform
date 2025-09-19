using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @interfaceObject on OBJECT
/// </code>
///
/// The @interfaceObject directive provides meta information to the router that this entity
/// type defined within this subgraph is an interface in the supergraph.
/// This allows you to extend functionality of an interface across the supergraph
/// without having to implement (or even be aware of) all its implementing types.
///
/// <example>
/// type Foo @interfaceObject @key(fields: "ids") {
///   id: ID!
///   newCommonField: String
/// }
/// </example>
/// </summary>
public sealed class InterfaceObjectAttribute : ObjectTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type type)
        => descriptor.InterfaceObject();
}
