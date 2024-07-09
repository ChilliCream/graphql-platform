using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Resale;

[GraphQLName("Query")]
public class ResaleQuery
{
    public Viewer GetViewer() => new();

    [NodeResolver]
    public Product? GetProductById([ID] int id) => new(id);
}

public class Viewer
{
    [UsePaging]
    public List<RecommendedResalableProduct> GetRecommendedResalableProducts()
    {
        return new()
        {
            new RecommendedResalableProduct(new Product(1)),
            // This product doesn't exist on the products subgraph
            new RecommendedResalableProduct(new Product(5)),
            new RecommendedResalableProduct(new Product(3))
        };
    }
}

public record RecommendedResalableProduct(Product Product);

public record Product([property: ID] int Id);
