namespace Products;

public class ProductRepository
{
    private readonly Dictionary<string, Product> _products;

    public ProductRepository()
    {
        _products = CreateProducts().ToDictionary(product => product.Upc);
    }

    public IEnumerable<Product> GetTop(int amount)
        => _products.Values.Take(amount);

    public Product GetById(string upc)
        => _products[upc];

    private static IEnumerable<Product> CreateProducts()
    {
        yield return new Product("1", "Table", 899, 100);
        yield return new Product("2", "Couch", 1299, 1000);
        yield return new Product("3", "Chair", 54, 50);
    }
}
