using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Descriptor for the <c>Animal</c> interface in <c>subgraph-a</c>.
/// Declares <c>id: ID!</c>.
/// </summary>
public sealed class AnimalInterfaceType : InterfaceType<IAnimal>
{
    protected override void Configure(IInterfaceTypeDescriptor<IAnimal> descriptor)
    {
        descriptor.Name("Animal");
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
    }
}
