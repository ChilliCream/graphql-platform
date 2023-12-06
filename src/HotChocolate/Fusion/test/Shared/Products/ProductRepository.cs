namespace HotChocolate.Fusion.Shared.Products;

public sealed class ProductRepository
{
    private readonly Dictionary<int, Product> _products;

    public ProductRepository()
    {
        _products = new[]
        {
            new Product(1, "Table", 899, 100, new ProductDimension(250, 150)),
            new Product(2, "Couch", 1299, 1000, new ProductDimension(2500, 150)),
            new Product(3, "Chair", 54, 50, new ProductDimension(15, 30))
        }.ToDictionary(t => t.Id);
    }

    public IEnumerable<Product> GetTopProducts(int first)
        => _products.Values.OrderBy(t => t.Id).Take(first);

    public Product? GetProductById(int upc)
        => _products.TryGetValue(upc, out var product)
            ? product
            : null;
}
