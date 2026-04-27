using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// Apollo Federation descriptor for the <c>ProductList</c> entity owned by the
/// <c>list</c> subgraph (<c>@key(fields: "products { id pid }")</c>). Owns the
/// shareable <c>first</c> and <c>selected</c> fields.
/// </summary>
public sealed class ProductListType : ObjectType<ProductList>
{
    protected override void Configure(IObjectTypeDescriptor<ProductList> descriptor)
    {
        descriptor
            .Key("products { id pid }")
            .ResolveReferenceWith(_ => ResolveByProducts(default!));

        descriptor.Field(l => l.Products).Type<NonNullType<ListType<NonNullType<ProductType>>>>();
        descriptor.Field(l => l.First).Shareable().Type<ProductType>();
        descriptor.Field(l => l.Selected).Shareable().Type<ProductType>();
    }

    private static ProductList ResolveByProducts(
        [Map("products")] IReadOnlyList<Product> products)
    {
        var materialized = products
            .Select(static p => ListData.ResolveIdPid(p.Id, p.Pid))
            .ToArray();

        return new ProductList
        {
            Products = materialized,
            First = materialized.Length > 0 ? materialized[0] : null,
            Selected = materialized.Length > 1 ? materialized[^1] : null
        };
    }
}
