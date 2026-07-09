using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Descriptor for the <c>Media</c> interface in the <c>a</c> subgraph
/// (<c>interface Media @key(fields: "id")</c>). The reference resolver dispatches
/// to the concrete <c>Video</c> implementer so the gateway can recover the
/// concrete <c>__typename</c> for the <c>Media @interfaceObject</c> declared in
/// subgraph <c>c</c>.
/// </summary>
public sealed class MediaInterfaceType : InterfaceType<IMedia>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMedia> descriptor)
    {
        descriptor.Name("Media");
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Title).Type<StringType>();
    }

    private static IMedia ResolveById(string id) => AData.VideoById(id);
}
