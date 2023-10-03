using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.ProductDetails;

[GraphQLName("Query")]
public sealed class ProductDetailsQuery
{
    [NodeResolver]
    public Product? GetProductById(int id) => new(id);
}

public sealed record Product([property: ID<Product>] int Id)
{
    public string GetFormattedDetails(string productName) => $"A beautiful {productName} ...";
}
