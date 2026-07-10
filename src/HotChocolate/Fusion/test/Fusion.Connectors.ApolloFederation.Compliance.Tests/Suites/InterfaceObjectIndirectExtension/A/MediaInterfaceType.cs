using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Descriptor for the <c>Media</c> interface in the <c>a</c> subgraph
/// (<c>interface Media @key(fields: "id")</c>).
/// </summary>
public sealed class MediaInterfaceType : InterfaceType<IMedia>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMedia> descriptor)
    {
        descriptor.Name("Media");
        descriptor
            .Key("id");

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Title).Type<StringType>();
    }
}
