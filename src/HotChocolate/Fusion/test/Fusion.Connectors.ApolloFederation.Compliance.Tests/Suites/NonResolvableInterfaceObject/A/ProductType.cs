using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;

public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.InterfaceObject();

        descriptor.Key("id", resolvable: false);

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }
}
