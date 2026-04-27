using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// Root <c>Query</c> for the <c>category</c> subgraph. Exposes
/// <c>products: [Product] @shareable @provides(...)</c> which returns
/// products with their categories, names, and nested subcategories
/// resolved inline.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Shareable()
            .Type<ListType<ProductType>>()
            .Provides("categories { id name subCategories { id name } }")
            .Resolve(_ =>
            {
                var products = new List<Product>();

                foreach (var id in new[] { "p1", "p2" })
                {
                    products.Add(CategoryData.BuildProduct(id));
                }

                return products;
            });
    }
}
