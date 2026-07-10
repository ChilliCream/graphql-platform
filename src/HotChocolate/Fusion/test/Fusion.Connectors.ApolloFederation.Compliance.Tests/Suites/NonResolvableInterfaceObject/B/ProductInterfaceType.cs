using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

public sealed class ProductInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Product");

        descriptor.Key("id");

        descriptor.Field("id").Type<NonNullType<IdType>>();
    }
}
