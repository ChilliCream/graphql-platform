using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Apollo Federation descriptor for the <c>SecondAddress</c> entity in subgraph <c>b</c>.
/// </summary>
public sealed class SecondAddressType : ObjectType<SecondAddress>
{
    protected override void Configure(IObjectTypeDescriptor<SecondAddress> descriptor)
    {
        descriptor
            .Implements<AddressInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.City).Shareable().Type<StringType>();
    }

    private static SecondAddress? ResolveById(string id)
        => new SecondAddress { Id = id };
}
