using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// Apollo Federation descriptor for the <c>ProductList</c> entity owned by the
/// <c>price</c> subgraph
/// (<c>@key(fields: "products { id pid category { id tag } } selected { id }")</c>).
/// The key resolver reconstructs the list plus the <c>selected</c> product from
/// the composite key payload.
/// </summary>
public sealed class ProductListType : ObjectType<ProductList>
{
    protected override void Configure(IObjectTypeDescriptor<ProductList> descriptor)
    {
        descriptor
            .Key("products { id pid category { id tag } } selected { id }")
            .ResolveReferenceWith(_ => ResolveByKey(default!, default));

        descriptor.Field(l => l.Products).Type<NonNullType<ListType<NonNullType<ProductType>>>>();
        descriptor.Field(l => l.First).Shareable().Type<ProductType>();
        descriptor.Field(l => l.Selected).Shareable().Type<ProductType>();
    }

    private static ProductList ResolveByKey(
        [Map("products")] IReadOnlyList<Product> products,
        [Map("selected.id")] string? selectedId)
    {
        var materialized = products
            .Select(static p =>
                PriceData.ResolveByKey(
                    p.Id,
                    p.Pid,
                    p.Category?.Id,
                    p.Category?.Tag))
            .ToArray();

        return new ProductList
        {
            Products = materialized,
            First = materialized.Length > 0 ? materialized[0] : null,
            Selected = selectedId is null
                ? null
                : materialized.FirstOrDefault(
                    p => p.Id.Equals(selectedId, StringComparison.Ordinal))
        };
    }
}
