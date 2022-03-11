namespace RemovedDirectives;

public class Query
{
    public IEnumerable<Product> GetTopProducts(
        ProductRepository productRepository,
        int? first)
        => productRepository.GetTop(first ?? 5);
}
