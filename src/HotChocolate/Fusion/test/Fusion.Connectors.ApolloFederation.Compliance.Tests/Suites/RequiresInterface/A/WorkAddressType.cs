using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Apollo Federation descriptor for the <c>WorkAddress</c> entity in subgraph <c>a</c>.
/// </summary>
public sealed class WorkAddressType : ObjectType<WorkAddress>
{
    protected override void Configure(IObjectTypeDescriptor<WorkAddress> descriptor)
    {
        descriptor
            .Implements<AddressInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.City).Shareable().Type<StringType>();
    }

    private static WorkAddress? ResolveById(string id)
        => AData.AddressesById.TryGetValue(id, out var addr) && addr is WorkAddress work
            ? work
            : null;
}
