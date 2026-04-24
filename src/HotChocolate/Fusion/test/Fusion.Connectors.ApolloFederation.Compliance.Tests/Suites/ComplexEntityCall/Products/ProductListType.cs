using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// Apollo Federation descriptor for the <c>ProductList</c> entity owned by the
/// <c>products</c> subgraph (<c>@key(fields: "products { id }")</c>).
/// </summary>
public sealed class ProductListType : ObjectType<ProductList>
{
    protected override void Configure(IObjectTypeDescriptor<ProductList> descriptor)
    {
        descriptor
            .Key("products { id }")
            .ResolveReferenceWith(_ => ResolveByProducts(default!));

        descriptor.Field(l => l.Products).Type<NonNullType<ListType<NonNullType<ProductType>>>>();
    }

    private static ProductList ResolveByProducts([Map("products")] IReadOnlyList<Product> products)
        => new() { Products = [.. products.Select(p => ResolveProduct(p.Id))] };

    private static Product ResolveProduct(string id)
        => ProductsData.ById.TryGetValue(id, out var product)
            ? product
            : new Product { Id = id, CategoryId = string.Empty };
}
