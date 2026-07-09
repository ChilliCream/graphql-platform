using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Descriptor for the <c>Media</c> interface in <c>subgraph-c</c>.
/// Declares <c>id: ID!</c> and <c>animals: [Animal]</c>.
/// </summary>
public sealed class MediaInterfaceType : InterfaceType<IMedia>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMedia> descriptor)
    {
        descriptor.Name("Media");
        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Animals).Type<ListType<AnimalInterfaceType>>();
    }
}
