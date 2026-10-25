using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Descriptor for the <c>Address</c> interface in subgraph <c>b</c>.
/// Declares <c>id: ID!</c>.
/// </summary>
public sealed class AddressInterfaceType : InterfaceType<IAddress>
{
    protected override void Configure(IInterfaceTypeDescriptor<IAddress> descriptor)
    {
        descriptor.Name("Address");
        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
    }
}
