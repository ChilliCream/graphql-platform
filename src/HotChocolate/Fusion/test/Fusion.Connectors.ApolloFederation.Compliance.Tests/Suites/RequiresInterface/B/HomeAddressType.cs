using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Apollo Federation descriptor for the <c>HomeAddress</c> entity in subgraph <c>b</c>.
/// </summary>
public sealed class HomeAddressType : ObjectType<HomeAddress>
{
    protected override void Configure(IObjectTypeDescriptor<HomeAddress> descriptor)
    {
        descriptor
            .Implements<AddressInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.City).Shareable().Type<StringType>();
    }

    private static HomeAddress? ResolveById(string id)
        => BData.AddressesById.TryGetValue(id, out var addr) && addr is HomeAddress home
            ? home
            : null;
}
