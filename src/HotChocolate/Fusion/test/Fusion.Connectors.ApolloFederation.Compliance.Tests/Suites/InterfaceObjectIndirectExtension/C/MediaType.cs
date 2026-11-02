using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Media</c> stand-in object in the
/// <c>c</c> subgraph. The <c>@interfaceObject</c> directive marks this object
/// as the supergraph <c>Media</c> interface, keyed by <c>id</c>, allowing the
/// subgraph to extend the interface without being aware of its implementations.
/// </summary>
public sealed class MediaType : ObjectType<Media>
{
    protected override void Configure(IObjectTypeDescriptor<Media> descriptor)
    {
        descriptor
            .InterfaceObject()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
    }

    private static Media ResolveById(string id) => new() { Id = id };
}
