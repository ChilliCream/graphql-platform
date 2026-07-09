using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

public sealed class BreadType : ObjectType<Bread>
{
    protected override void Configure(IObjectTypeDescriptor<Bread> descriptor)
    {
        descriptor.Name("Bread");

        descriptor.Implements<ProductInterfaceType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Name).Type<NonNullType<StringType>>();
    }

    private static Bread? ResolveById(string id)
        => BData.ProductsById.TryGetValue(id, out var bread) ? bread : null;
}
