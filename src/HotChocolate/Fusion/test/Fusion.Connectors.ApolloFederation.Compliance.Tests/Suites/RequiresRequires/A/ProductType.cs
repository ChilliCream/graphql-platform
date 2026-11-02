using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in
/// subgraph <c>a</c>. The <c>price</c> field is <c>@inaccessible</c>,
/// meaning it is available for composition but hidden from the public API.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Price).Type<NonNullType<FloatType>>().Inaccessible();
    }

    private static Product? ResolveById(string id)
        => ProductData.ById.TryGetValue(id, out var product) ? product : null;
}
