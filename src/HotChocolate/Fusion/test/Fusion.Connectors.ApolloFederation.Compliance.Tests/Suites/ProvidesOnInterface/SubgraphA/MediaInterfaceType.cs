using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Descriptor for the <c>Media</c> interface in <c>subgraph-a</c>.
/// Declares <c>id: ID!</c>.
/// </summary>
public sealed class MediaInterfaceType : InterfaceType<IMedia>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMedia> descriptor)
    {
        descriptor.Name("Media");
        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
    }
}
