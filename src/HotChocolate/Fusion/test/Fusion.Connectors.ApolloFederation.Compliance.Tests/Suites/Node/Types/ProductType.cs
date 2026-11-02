using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>types</c> subgraph (<c>@key(fields: "id") @shareable</c>). Owns
/// <c>name</c> and <c>price</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Shareable()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Price).Type<NonNullType<FloatType>>();
    }

    private static Product? ResolveById(string id)
        => TypesData.ProductsById.TryGetValue(id, out var p) ? p : null;
}
