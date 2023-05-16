using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Products;

[GraphQLName("Query")]
public sealed class ProductQuery
{
    public IEnumerable<Product> GetTopProducts(
        int first,
        [Service] ProductRepository repository)
        => repository.GetTopProducts(first);

    [NodeResolver]
    public Product? GetProductById(
        int id,
        [Service] ProductRepository repository)
        => repository.GetProductById(id);
}
