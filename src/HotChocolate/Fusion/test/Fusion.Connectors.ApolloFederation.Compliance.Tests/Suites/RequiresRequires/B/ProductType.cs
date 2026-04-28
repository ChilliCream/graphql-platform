using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in
/// subgraph <c>b</c>. Owns <c>hasDiscount</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.HasDiscount).Type<NonNullType<BooleanType>>();
    }

    private static Product? ResolveById(string id)
        => ProductData.ById.TryGetValue(id, out var product) ? product : null;
}
