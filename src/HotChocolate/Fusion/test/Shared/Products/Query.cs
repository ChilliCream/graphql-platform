namespace HotChocolate.Fusion.Shared.Products;

[GraphQLName("Query")]
public class ProductQuery
{
    public IEnumerable<Product> GetTopProducts(
        int first,
        [Service] ProductRepository repository) =>
        repository.GetTopProducts(first);

    public Product GetProductById(
        int upc,
        [Service] ProductRepository repository) =>
        repository.GetProduct(upc);
}
