using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

public sealed class ProductInterfaceType : InterfaceType<IProduct>
{
    protected override void Configure(IInterfaceTypeDescriptor<IProduct> descriptor)
    {
        descriptor.Name("Product");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
    }

    private static IProduct? ResolveById(string id)
        => BData.ProductsById.TryGetValue(id, out var bread) ? bread : null;
}
