using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Descriptor for the <c>Animal</c> interface in <c>subgraph-b</c>.
/// Declares <c>id: ID!</c> and <c>name: String</c>.
/// </summary>
public sealed class AnimalInterfaceType : InterfaceType<IAnimal>
{
    protected override void Configure(IInterfaceTypeDescriptor<IAnimal> descriptor)
    {
        descriptor.Name("Animal");
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Name).Type<StringType>();
    }
}
